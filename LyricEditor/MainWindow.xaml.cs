using ATL;
using LyricEditor.Lyric;
using LyricEditor.UserControls;
using LyricEditor.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using Forms = System.Windows.Forms;

namespace LyricEditor
{
    public enum LrcPanelType
    {
        LrcLinePanel,
        LrcTextPanel
    }

    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            LrcLinePanel = (LrcLineView)LrcPanelContainer.Content;
            LrcTextPanel = new LrcTextView();

            //SwitchLrcPanelButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

            Timer = new DispatcherTimer();
            Timer.Tick += new EventHandler(Timer_Tick);
            Timer.Interval = new TimeSpan(0, 0, 0, 0, 20);
            Timer.Start();

            //ImportMedia("D:\\C#\\LyricEditor\\卓依婷 - 童年.mp3");
        }

        #region 成员变量

        private LrcPanelType CurrentLrcPanel = LrcPanelType.LrcLinePanel;

        /// <summary>
        /// 表示播放器是否正在播放
        /// </summary>
        private bool isPlaying = false;

        private LrcLineView LrcLinePanel;
        private LrcTextView LrcTextPanel;

        public TimeSpan ShortTimeShift { get; private set; } = new TimeSpan(0, 0, 2);
        public TimeSpan LongTimeShift { get; private set; } = new TimeSpan(0, 0, 5);

        private string audioPath;
        private string lrcPath;

        private string configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().GetName().Name),
            $"{Assembly.GetExecutingAssembly().GetName().Name}.config");

        private List<string> tmpList = [];

        #endregion

        #region 计时器

        DispatcherTimer Timer;

        /// <summary>
        /// 每个计时器时刻，更新时间轴上的全部信息
        /// </summary>
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (!IsMediaAvailable)
                return;

            var current = MediaPlayer.Position;
            CurrentTimeText.Text = $"{current.Minutes:00}:{current.Seconds:00}";

            TimeBackground.Value =
                MediaPlayer.Position.TotalSeconds
                / MediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
            CurrentLrcText.Text = LrcManager.Instance.GetNearestLrc(MediaPlayer.Position);
        }

        #endregion

        #region 媒体播放器

        private bool IsMediaAvailable
        {
            get
            {
                if (MediaPlayer.Source is null)
                    return false;
                else
                    return MediaPlayer.HasAudio && MediaPlayer.NaturalDuration.HasTimeSpan;
            }
        }

        private void Play()
        {
            if (!IsMediaAvailable)
                return;

            MediaPlayer.Play();

            PlayButton.Tag = true;

            isPlaying = true;
        }

        private void Pause()
        {
            if (!IsMediaAvailable)
                return;

            MediaPlayer.Pause();

            PlayButton.Tag = false;

            isPlaying = false;
        }

        private void Stop()
        {
            if (!IsMediaAvailable)
                return;

            MediaPlayer.Stop();

            PlayButton.Tag = false;

            isPlaying = false;
        }

        #endregion

        #region 内部方法

        private void UpdateLrcView()
        {
            LrcLinePanel.UpdateLrcPanel();
            LrcTextPanel.Text = LrcManager.Instance.ToString();
        }

        public void UpdateLrcViewInvoke()
        {
            LrcLinePanel.Dispatcher.Invoke(() =>
            {
                LrcLinePanel.UpdateLrcPanel();
                LrcTextPanel.Text = LrcManager.Instance.ToString();
            });
        }

        private void ImportMedia(string filename)
        {
            if (audioPath == filename) return;
            else
            {
                audioPath = filename;
            }

            try
            {
                string tmpPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + Path.GetExtension(filename));
                tmpList.Add(tmpPath);
                byte[] bytes = File.ReadAllBytes(filename);
                File.WriteAllBytes(tmpPath, bytes);

                MediaPlayer.Source = new Uri(tmpPath);
                MediaPlayer.Stop();

                Track theFile = new(tmpPath);
                string title = theFile.Title;
                if (string.IsNullOrWhiteSpace(title))
                    title = Path.GetFileNameWithoutExtension(audioPath);
                TitleBox.Text = title;

                string performers = theFile.Artist;
                PerformerBox.Text = performers;
                Title = $"歌词编辑器 {performers} - {title}";

                if (theFile.EmbeddedPictures.Count != 0)
                {
                    PictureInfo embeddedPictures = theFile.EmbeddedPictures[0];
                    BitmapImage image = new();
                    image.BeginInit();
                    image.StreamSource = new MemoryStream(embeddedPictures.PictureData);
                    image.EndInit();
                    Cover.Source = image;
                }
                else
                {
                    Cover.Source = ResourceHelper.GetIcon("disc.png");
                }

                string album = theFile.Album;
                AlbumBox.Text = album;

                if (theFile.AdditionalFields.TryGetValue("LYRICS", out string value))
                {
                    LrcManager.Instance.LoadFromText(value);
                    UpdateLrcView();
                }
            }
            catch
            {
                Cover.Source = ResourceHelper.GetIcon("disc.png");
                audioPath = string.Empty;
            }
        }

        #endregion

        #region 菜单按钮

        /// <summary>
        /// 界面读取，用于初始化
        /// </summary>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (Application.Current.Properties["arg1"] != null)
            {
                ImportMedia(Application.Current.Properties["arg1"].ToString());
            }

            #region 读取配置

            if (!File.Exists(configPath))
            {
                if (!Directory.Exists(Path.GetDirectoryName(configPath))) { Directory.CreateDirectory(Path.GetDirectoryName(configPath)); }
                Stream stream = Application.GetResourceStream(new Uri("Appp.config", UriKind.Relative)).Stream;
                using FileStream sw = File.Create(configPath);
                stream.CopyTo(sw);
            }

            XmlDocument xmlDocument = new();
            xmlDocument.Load(configPath);

            XmlElement appSettings = xmlDocument["configuration"]["appSettings"];
            foreach (XmlElement el in appSettings)
            {
                switch (el.GetAttribute("key"))
                {
                    // 退出时自动缓存
                    case "AutoSaveTemp":
                        AutoSaveTemp.IsChecked = bool.Parse(el.GetAttribute("value"));
                        break;
                    // 导出 UTF-8
                    case "ExportUTF8":
                        ExportUTF8.IsChecked = bool.Parse(el.GetAttribute("value"));
                        break;
                    // 时间取近似值
                    case "ApproxTime":
                        LrcLine.IsShort =
                        ApproxTime.IsChecked =
                            bool.Parse(el.GetAttribute("value"));
                        break;
                    // 时间偏差（改变 Text 会触发 TextChanged 事件，下同）
                    case "TimeOffset":
                        TimeOffset.Text = el.GetAttribute("value");
                        break;
                    // 快进快退
                    case "ShortTimeShift":
                        ShortShift.Text = el.GetAttribute("value");
                        break;
                    case "LongTimeShift":
                        LongShift.Text = el.GetAttribute("value");
                        break;
                }
            }

            #endregion

            // 打开缓存文件
            if (AutoSaveTemp.IsChecked && File.Exists(FileHelper.TempFileName))
            {
                LrcManager.Instance.LoadFromFile(FileHelper.TempFileName);
                UpdateLrcView();
            }
        }

        /// <summary>
        /// 程序退出，关闭计时器，修改配置文件
        /// </summary>
        private void Window_Closed(object sender, EventArgs e)
        {
            Timer.Stop();

            //清理临时文件
            foreach (string tmp in tmpList) File.Delete(tmp);

            // 保存配置文件
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(configPath);

            XmlElement appSettings = xmlDocument["configuration"]["appSettings"];
            foreach (XmlElement el in appSettings)
            {
                switch (el.GetAttribute("key"))
                {
                    // 退出时自动缓存
                    case "AutoSaveTemp":
                        el.SetAttribute("value", AutoSaveTemp.IsChecked.ToString());
                        break;
                    // 导出 UTF-8
                    case "ExportUTF8":
                        el.SetAttribute("value", ExportUTF8.IsChecked.ToString());
                        break;
                    // 时间取近似值
                    case "ApproxTime":
                        el.SetAttribute("value", LrcLinePanel.ApproxTime.ToString());
                        break;
                    // 时间偏差（改变 Text 会触发 TextChanged 事件，下同）
                    case "TimeOffset":
                        el.SetAttribute("value", (-LrcLinePanel.TimeOffset.TotalMilliseconds).ToString());
                        break;
                    // 快进快退
                    case "ShortTimeShift":
                        el.SetAttribute("value", ShortTimeShift.TotalSeconds.ToString());
                        break;
                    case "LongTimeShift":
                        el.SetAttribute("value", LongTimeShift.TotalSeconds.ToString());
                        break;
                }
            }
            xmlDocument.Save(configPath);

            // 保存缓存
            if (AutoSaveTemp.IsChecked)
            {
                Encoding encoding = ExportUTF8.IsChecked ? Encoding.UTF8 : Encoding.Default;
                File.WriteAllText(FileHelper.TempFileName, LrcManager.Instance.ToString(), encoding);
            }
            else if (File.Exists(FileHelper.TempFileName))
            {
                File.Delete(FileHelper.TempFileName);
            }
        }

        /// <summary>
        /// 导入音频文件
        /// </summary>
        private void ImportMedia_Click(object sender, RoutedEventArgs e)
        {
            Forms.OpenFileDialog ofd = new()
            {
                Filter = "媒体文件|*.mp3;*.wav;*.3gp;*.mp4;*.avi;*.wmv;*.wma;*.aac|所有文件|*.*"
            };

            if (ofd.ShowDialog() == Forms.DialogResult.OK)
            {
                ImportMedia(ofd.FileName);
            }
        }

        /// <summary>
        /// 导入歌词文件
        /// </summary>
        private void ImportLyric_Click(object sender, RoutedEventArgs e)
        {
            Forms.OpenFileDialog ofd = new()
            {
                Filter = "歌词文件|*.lrc;*.txt|所有文件|*.*"
            };

            if (ofd.ShowDialog() == Forms.DialogResult.OK)
            {
                LrcManager.Instance.LoadFromFile(ofd.FileName);
                UpdateLrcView();
                lrcPath = ofd.FileName;
            }
        }

        /// <summary>
        /// 导入歌词文件
        /// </summary>
        private void ImportLyricFromTag_Click(object sender, RoutedEventArgs e)
        {
            Track theFile = new(audioPath);
            if (theFile.AdditionalFields.TryGetValue("LYRICS", out string value))
            {
                LrcManager.Instance.LoadFromText(value);
                UpdateLrcView();
            }   
        }

        /// <summary>
        /// 将歌词保存为文本文件
        /// </summary>
        private void ExportLyric_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(lrcPath))
            {
                Encoding encoding = ExportUTF8.IsChecked ? Encoding.UTF8 : Encoding.Default;
                File.WriteAllText(lrcPath, LrcManager.Instance.ToString(), encoding);
            }
            else
            {
                Forms.SaveFileDialog ofd = new()
                {
                    Filter = "歌词文件|*.lrc|文本文件|*.txt|所有文件|*.*",
                    FileName = $"{PerformerBox.Text} - {TitleBox.Text}.lrc",
                    DefaultExt = "lrc"
                };
                if (ofd.ShowDialog() == Forms.DialogResult.OK)
                {
                    Encoding encoding = ExportUTF8.IsChecked ? Encoding.UTF8 : Encoding.Default;
                    File.WriteAllText(ofd.FileName, LrcManager.Instance.ToString(), encoding);
                }
            }
        }

        /// <summary>
        /// 将歌词保存至标签
        /// </summary>
        private void ExportLyricToTag_Click(object sender, RoutedEventArgs e)
        {
            Track theFile = new(audioPath);
            theFile.AdditionalFields["LYRICS"] = LrcManager.Instance.ToString();
            theFile.Save();
        }

        /// <summary>
        /// 从剪贴板粘贴歌词文本
        /// </summary>
        private void ImportLyricFromClipboard_Click(object sender, RoutedEventArgs e)
        {
            LrcManager.Instance.LoadFromText(Clipboard.GetText());
            UpdateLrcView();
        }

        /// <summary>
        /// 将歌词文本复制到剪贴板
        /// </summary>
        private void ExportLyricToClipboard_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(LrcManager.Instance.ToString());
        }

        /// <summary>
        /// 配置选项发生变化
        /// </summary>
        private void Settings_Checked(object sender, RoutedEventArgs e)
        {
            var item = sender as MenuItem;
            switch (item.Name)
            {
                case "ApproxTime":
                    LrcLinePanel.ApproxTime = item.IsChecked;
                    LrcLine.IsShort = item.IsChecked;
                    if (LrcPanelContainer.Content is LrcLineView view)
                    {
                        view.LrcLinePanel.Items.Refresh();
                    }
                    break;
            }
        }

        /// <summary>
        /// 打开媒体文件后，更新时间轴上的总时间
        /// </summary>
        private void MediaPlayer_MediaOpened(object sender, RoutedEventArgs e)
        {
            var totalTime = MediaPlayer.NaturalDuration.TimeSpan;
            TotalTimeText.Text = $"{totalTime.Minutes:00}:{totalTime.Seconds:00}";
            CurrentTimeText.Text = "00:00";
            Pause();
        }

        /// <summary>
        /// 处理窗口的文件拖入事件
        /// </summary>
        public void Window_Drop(object sender, DragEventArgs e)
        {
            string[] filePath = ((string[])e.Data.GetData(DataFormats.FileDrop));

            foreach (var file in filePath)
            {
                string ext = Path.GetExtension(file).ToLower();
                if (FileHelper.MediaExtensions.Contains(ext))
                {
                    ImportMedia(file);
                }
                else if (FileHelper.LyricExtensions.Contains(ext))
                {
                    LrcManager.Instance.LoadFromFile(file);
                    UpdateLrcView();
                    lrcPath = file;
                }
            }
        }

        public void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effects = DragDropEffects.Link;
            else
                e.Effects = DragDropEffects.None;
        }

        /// <summary>
        /// 关闭按钮
        /// </summary>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TimeOffset_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (LrcLinePanel is null)
                return;
            if (int.TryParse(TimeOffset.Text, out int offset))
            {
                LrcLinePanel.TimeOffset = new TimeSpan(0, 0, 0, 0, -offset);
            }
        }

        private void TimeShift_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (LrcLinePanel is null)
                return;

            TextBox box = sender as TextBox;
            if (int.TryParse(box.Text, out int value))
            {
                switch (box.Name)
                {
                    case "ShortShift":
                        ShortTimeShift = new TimeSpan(0, 0, value);
                        break;

                    case "LongShift":
                        LongTimeShift = new TimeSpan(0, 0, value);
                        break;
                }
            }
        }

        /// <summary>
        /// 重置所有歌词行的时间
        /// </summary>
        private void ResetAllTime_Click(object sender, RoutedEventArgs e)
        {
            LrcLinePanel.ResetAllTime();
        }

        /// <summary>
        /// 平移所有歌词行的时间
        /// </summary>
        private void ShiftAllTime_Click(object sender, RoutedEventArgs e)
        {
            string str = InputBox.Show(this, "输入", "请输入时间偏移量(ms)：");
            if (double.TryParse(str, out double offset))
            {
                LrcLinePanel.ShiftAllTime(new TimeSpan(0, 0, 0, 0, (int)(offset)));
            }
        }

        #endregion

        #region 工具按钮

        /// <summary>
        /// 切换面板
        /// </summary>
        private void SwitchLrcPanel_Click(object sender, RoutedEventArgs e)
        {
            switch (CurrentLrcPanel)
            {
                // 切换回纯文本
                case LrcPanelType.LrcLinePanel:
                    UpdateLrcView();
                    LrcPanelContainer.Content = LrcTextPanel;
                    CurrentLrcPanel = LrcPanelType.LrcTextPanel;
                    // 切换到文本编辑模式时，按钮旋转180度，且相关按钮不可用
                    ((Image)((Button)sender).Content).LayoutTransform = new RotateTransform(180);
                    ToolsForLrcLineOnly.Visibility = Visibility.Collapsed;
                    FlagButton.Visibility = Visibility.Hidden;
                    ClearAllTime.IsEnabled = true;
                    SortTime.IsEnabled = false;
                    break;

                // 切换回歌词行
                case LrcPanelType.LrcTextPanel:
                    // 在回到歌词行模式前，要检查当前文本能否进行正确转换
                    if (!LrcManager.Instance.LoadFromText(LrcTextPanel.Text))
                        return;
                    UpdateLrcView();
                    LrcPanelContainer.Content = LrcLinePanel;
                    CurrentLrcPanel = LrcPanelType.LrcLinePanel;
                    // 切换到文本编辑模式时，按钮旋转角度复原，且相关按钮可用
                    ((Image)((Button)sender).Content).LayoutTransform = new RotateTransform(0);
                    ToolsForLrcLineOnly.Visibility = Visibility.Visible;
                    FlagButton.Visibility = Visibility.Visible;
                    ClearAllTime.IsEnabled = false;
                    SortTime.IsEnabled = true;
                    break;
            }
        }

        /// <summary>
        /// 播放按钮
        /// </summary>
        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isPlaying)
            {
                Play();
            }
            else
            {
                Pause();
            }
        }

        /// <summary>
        /// 停止按钮
        /// </summary>
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        /// <summary>
        /// 快进快退
        /// </summary>
        private void TimeShift_Click(object sender, RoutedEventArgs e)
        {
            if (!IsMediaAvailable)
                return;

            switch (((Button)sender).Name)
            {
                case "ShortShiftLeft":
                    MediaPlayer.Position -= ShortTimeShift;
                    break;
                case "ShortShiftRight":
                    MediaPlayer.Position += ShortTimeShift;
                    break;
                case "LongShiftLeft":
                    MediaPlayer.Position -= LongTimeShift;
                    break;
                case "LongShiftRight":
                    MediaPlayer.Position += LongTimeShift;
                    break;
            }
        }

        /// <summary>
        /// 播放速度
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MediaPlayer.SpeedRatio = e.NewValue;
        }

        /// <summary>
        /// 搜索歌词
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SerachLyric_Click(object sender, RoutedEventArgs e)
        {
            if (MediaPlayer.Source is null) return;

            string performers = PerformerBox.Text;
            string title = TitleBox.Text;

            SearchLyric searchLyric = new(this, title, performers);

            searchLyric.Show();
            searchLyric.Search_Button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        }

        /// <summary>
        /// 时间轴点击
        /// </summary>
        private void TimeClickBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsMediaAvailable)
                return;

            double current = e.GetPosition(TimeClickBar).X;
            double percent = current / TimeClickBar.ActualWidth;
            TimeBackground.Value = percent;

            MediaPlayer.Position = new TimeSpan(
                0,
                0,
                0,
                0,
                (int)(MediaPlayer.NaturalDuration.TimeSpan.TotalMilliseconds * percent)
            );
        }

        /// <summary>
        /// 撤销
        /// </summary>
        private void Undo_Click(object sender, RoutedEventArgs e)
        {
            switch (CurrentLrcPanel)
            {
                case LrcPanelType.LrcLinePanel:
                    LrcLinePanel.Undo();
                    break;

                case LrcPanelType.LrcTextPanel:
                    LrcTextPanel.LrcTextPanel.Undo();
                    break;
            }
        }

        /// <summary>
        /// 重做
        /// </summary>
        private void Redo_Click(object sender, RoutedEventArgs e)
        {
            switch (CurrentLrcPanel)
            {
                case LrcPanelType.LrcLinePanel:
                    LrcLinePanel.Redo();
                    break;

                case LrcPanelType.LrcTextPanel:
                    LrcTextPanel.LrcTextPanel.Redo();
                    break;
            }
        }

        /// <summary>
        /// 将媒体播放位置应用到当前歌词行
        /// </summary>
        private void SetTime_Click(object sender, RoutedEventArgs e)
        {
            if (!IsMediaAvailable)
                return;
            if (CurrentLrcPanel != LrcPanelType.LrcLinePanel)
                return;

            LrcLinePanel.SetCurrentLineTime(MediaPlayer.Position);
        }

        /// <summary>
        /// 添加新行
        /// </summary>
        private void AddNewLine_Click_Up(object sender, RoutedEventArgs e)
        {
            LrcLinePanel.AddNewLineUp(MediaPlayer.Position);
        }

        /// <summary>
        /// 添加新行
        /// </summary>
        private void AddNewLine_Click_Down(object sender, RoutedEventArgs e)
        {
            LrcLinePanel.AddNewLineDown(MediaPlayer.Position);
        }

        /// <summary>
        /// 删除行
        /// </summary>
        private void DeleteLine_Click(object sender, RoutedEventArgs e)
        {
            LrcLinePanel.DeleteLine();
        }

        /// <summary>
        /// 上移一行
        /// </summary>
        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            LrcLinePanel.MoveUp();
        }

        /// <summary>
        /// 下移一行
        /// </summary>
        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            LrcLinePanel.MoveDown();
        }

        /// <summary>
        /// 清空所有时间标记
        /// </summary>
        private void ClearAllTime_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentLrcPanel != LrcPanelType.LrcTextPanel)
                return;
            LrcTextPanel.ClearAllTime();
        }

        /// <summary>
        /// 强制排序
        /// </summary>
        private void SortTime_Click(object sender, RoutedEventArgs e)
        {
            LrcManager.Instance.Sort();
            LrcLinePanel.UpdateLrcPanel();
        }

        /// <summary>
        /// 清空全部内容
        /// </summary>
        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            switch (CurrentLrcPanel)
            {
                case LrcPanelType.LrcLinePanel:
                    LrcManager.Instance.Clear();
                    LrcLinePanel.UpdateLrcPanel();
                    break;

                case LrcPanelType.LrcTextPanel:
                    LrcTextPanel.Clear();
                    break;
            }
        }

        /// <summary>
        /// 软件信息
        /// </summary>
        private void Info_Click(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show(Properties.Resources.Info, "相关信息", MessageBoxButton.OKCancel);
            if (res == MessageBoxResult.OK)
                Process.Start("https://zhuanlan.zhihu.com/p/32588196");
        }

        #endregion

        #region 快捷键

        private void SetTimeShortcut_Executed(object sender, ExecutedRoutedEventArgs e) =>
            SetTime_Click(this, e);

        private void HelpShortcut_Executed(object sender, ExecutedRoutedEventArgs e) =>
            Info_Click(this, e);

        private void PlayShortcut_Executed(object sender, ExecutedRoutedEventArgs e) =>
            PlayButton_Click(this, e);

        private void UndoShortcut_Executed(object sender, ExecutedRoutedEventArgs e) =>
            Undo_Click(this, e);

        private void RedoShortcut_Executed(object sender, ExecutedRoutedEventArgs e) =>
            Redo_Click(this, e);

        private void InsertShortcut_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (CurrentLrcPanel == LrcPanelType.LrcLinePanel)
            {
                AddNewLine_Click_Up(this, null);
            }
        }

        #endregion

    }
}

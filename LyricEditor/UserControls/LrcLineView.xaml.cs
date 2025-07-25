﻿using LyricEditor.Lyric;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LyricEditor.UserControls
{
    public partial class LrcLineView : UserControl
    {
        public LrcLineView()
        {
            InitializeComponent();

            LrcLinePanel.Items.Clear();
            CurrentTimeText.Clear();
            CurrentLrcText.Clear();
        }

        public TimeSpan TimeOffset { get; set; } = new TimeSpan(0, 0, 0, 0, -150);
        public bool ApproxTime { get; set; } = false;

        private TimeSpan GetApproxTime(TimeSpan time)
        {
            return new TimeSpan(
                time.Days,
                time.Hours,
                time.Minutes,
                time.Seconds,
                time.Milliseconds / 10 * 10
            );
        }

        public bool HasSelection
        {
            get => SelectedIndex != -1;
        }
        public int SelectedIndex
        {
            get => LrcLinePanel.SelectedIndex;
            set => LrcLinePanel.SelectedIndex = value;
        }
        public LrcLine SelectedItem
        {
            get { return LrcLinePanel.SelectedItem as LrcLine; }
            set { LrcLinePanel.SelectedItem = value; }
        }
        public bool ReachEnd
        {
            get => SelectedIndex == LrcLinePanel.Items.Count - 1;
        }
        public bool ReachStart
        {
            get => SelectedIndex == 0;
        }

        /// <summary>
        /// 修改了单行的信息后，更新歌词列表的显示
        /// </summary>
        public void RefreshLrcPanel()
        {
            LrcLinePanel.Items.Refresh();
        }

        /// <summary>
        /// 同步 LrcManager.Instance 与歌词列表
        /// </summary>
        public void UpdateLrcPanel()
        {
            LrcManager.Instance.UpdateLrcList(LrcLinePanel);
        }

        /// <summary>
        /// 根据选择的行数更改下方文本框的内容
        /// </summary>
        public void UpdateBottomTextBoxes()
        {
            // 如果只选中了一项
            if (LrcLinePanel.SelectedItems.Count == 1)
            {
                LrcLine line = LrcLinePanel.SelectedItem as LrcLine;
                if (!(line.LrcTime is null))
                    CurrentTimeText.Text = line.LrcTimeText;
                else
                    CurrentTimeText.Clear();
                CurrentLrcText.Text = line.LrcText;
            }
        }

        /// <summary>
        /// 歌词窗口的选择项发生改变
        /// </summary>
        private void LrcLinePanel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!HasSelection)
                return;
            UpdateBottomTextBoxes();
        }

        /// <summary>
        /// 更改时间框的文本，更新主列表
        /// </summary>
        private void CurrentTime_Changed(object sender, TextChangedEventArgs e)
        {
            if (!HasSelection)
                return;

            int index = SelectedIndex;
            if (LrcHelper.TryParseTimeSpan(CurrentTimeText.Text, out TimeSpan time))
            {
                LrcManager.Instance.LrcList[index].LrcTime = time;
                ((LrcLine)LrcLinePanel.Items[index]).LrcTime = time;
                RefreshLrcPanel();
            }
            else if (string.IsNullOrWhiteSpace(CurrentTimeText.Text))
            {
                LrcManager.Instance.LrcList[index].LrcTime = null;
                ((LrcLine)LrcLinePanel.Items[index]).LrcTime = null;
                RefreshLrcPanel();
            }
        }

        /// <summary>
        /// 更改歌词框的文本，更新主列表
        /// </summary>
        private void CurrentLrc_Changed(object sender, TextChangedEventArgs e)
        {
            if (!HasSelection)
                return;

            int index = SelectedIndex;
            LrcManager.Instance.LrcList[index].LrcText = CurrentLrcText.Text;
            ((LrcLine)LrcLinePanel.Items[index]).LrcText = CurrentLrcText.Text;
            RefreshLrcPanel();
        }

        /// <summary>
        /// 在时间框中使用滚轮
        /// </summary>
        private void CurrentTimeText_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // 如果没有选中任意一行
            if (!HasSelection)
                return;
            // 如果当前时间栏为空
            if (string.IsNullOrWhiteSpace(CurrentTimeText.Text))
                return;
            // 如果选中行的时间不存在（信息行）
            // 下面这行理论上是不需要的，因为如果是信息行，那么时间栏应该就是空的
            //if (SelectedItem.LrcTime is null) return;

            int index = SelectedIndex;
            var currentTime = LrcManager.Instance.LrcList[index].LrcTime.Value.TotalSeconds;
            if (e.Delta > 0)
            {
                AdjustCurrentLineTime(new TimeSpan(0, 0, 0, 0, 50));
            }
            else
            {
                AdjustCurrentLineTime(new TimeSpan(0, 0, 0, 0, -50));
            }
        }

        /// <summary>
        /// 双击主列表，跳转播放时间
        /// </summary>
        private void LrcLinePanel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (!HasSelection)
                return;

            LrcLine line = LrcLinePanel.SelectedItem as LrcLine;
            if (!line.LrcTime.HasValue)
                return;

            TimeSpan linetime = line.LrcTime.Value;

            ((MainWindow)Application.Current.MainWindow).audioFile.CurrentTime = linetime;
            ((MainWindow)Application.Current.MainWindow).Timer_Tick_Set(linetime);
        }

        /// <summary>
        /// 在主列表上使用按键
        /// </summary>
        private void LrcLinePanel_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:
                    DeleteLine();
                    break;
                case Key.C:
                    if (Keyboard.Modifiers == ModifierKeys.Control) CopySelectedItemsToClipboard();
                    break;
                case Key.V:
                    if (Keyboard.Modifiers == ModifierKeys.Control) PasteClipboardToSelectedItems();
                    break;
            }
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DeleteLine();
        }

        private void CopyMenuItem_Click(object sender, RoutedEventArgs e)
        {
            CopySelectedItemsToClipboard();
        }

        private void PasteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            PasteClipboardToSelectedItems();
        }

        private void CopySelectedItemsToClipboard()
        {
            if (LrcLinePanel.SelectedItem != null && LrcLinePanel.SelectedItems.Count == 1)
            {
                LrcLine line = LrcLinePanel.SelectedItem as LrcLine;

                string copiedText = line.LrcText;

                // 将文本复制到剪贴板
                Clipboard.SetText(copiedText);
            }
        }

        private void PasteClipboardToSelectedItems()
        {
            if (LrcLinePanel.SelectedItem != null && LrcLinePanel.SelectedItems.Count == 1)
            {
                CurrentLrcText.Text = Clipboard.GetText();
            }
        }

        private void AdjustCurrentLineTime(TimeSpan delta)
        {
            int index = SelectedIndex;

            var currentTime = LrcManager.Instance.LrcList[index].LrcTime.Value.Add(delta);
            if (currentTime < TimeSpan.Zero)
                currentTime = TimeSpan.Zero;

            LrcManager.Instance.LrcList[index].LrcTime = currentTime;
            ((LrcLine)LrcLinePanel.Items[index]).LrcTime = currentTime;

            UpdateBottomTextBoxes();
        }

        public void SetCurrentLineTime(TimeSpan time)
        {
            if (!HasSelection)
                return;
            int index = SelectedIndex;

            // 判断是否为歌曲信息行
            if (!LrcManager.Instance.LrcList[index].LrcTime.HasValue)
                return;

            time += TimeOffset;
            if (time < TimeSpan.Zero)
                time = TimeSpan.Zero;

            // 更新选中行的时间
            LrcManager.Instance.LrcList[index].LrcTime = time;
            ((LrcLine)LrcLinePanel.Items[index]).LrcTime = time;

            RefreshLrcPanel();

            // 根据是否到达最后一行来设定下一个选中行
            if (!ReachEnd)
            {
                SelectedIndex++;
                LrcLinePanel.ScrollIntoView(LrcLinePanel.Items[index + 10 < LrcLinePanel.Items.Count ? index + 10 : SelectedIndex]);
            }
            else
            {
                //SelectedIndex = -1;
                //LrcLinePanel.ScrollIntoView(LrcLinePanel.Items[SelectedIndex]);
            }
        }

        public void ResetAllTime() => LrcManager.Instance.ResetAllTime(LrcLinePanel);

        public void ShiftAllTime(TimeSpan offset) => LrcManager.Instance.ShiftAllTime(LrcLinePanel, offset);

        public void Undo() => LrcManager.Instance.Undo(LrcLinePanel);

        public void Redo() => LrcManager.Instance.Redo(LrcLinePanel);

        public void AddNewLineUp(TimeSpan time)
        {
            LrcManager.Instance.AddNewLineUp(LrcLinePanel, time);
            int index = SelectedIndex;
            if (!ReachEnd)
            {
                SelectedIndex++;
                LrcLinePanel.ScrollIntoView(LrcLinePanel.Items[index + 10 < LrcLinePanel.Items.Count ? index + 10 : SelectedIndex]);
            }
        }

        public void AddNewLineDown(TimeSpan time)
        {
            LrcManager.Instance.AddNewLineDown(LrcLinePanel, time);
            int index = SelectedIndex;
            if (!ReachEnd)
            {
                SelectedIndex++;
                LrcLinePanel.ScrollIntoView(LrcLinePanel.Items[index + 10 < LrcLinePanel.Items.Count ? index + 10 : SelectedIndex]);
            }
        }

        public void DeleteLine() => LrcManager.Instance.DeleteLine(LrcLinePanel);

        public void MoveUpLyric() => LrcManager.Instance.MoveUp(LrcLinePanel);

        public void MoveDownLyric() => LrcManager.Instance.MoveDown(LrcLinePanel);

        public void AddTit(string tt) => LrcManager.Instance.AddTit(LrcLinePanel, tt);

        public void MoveUp()
        {
            int index = SelectedIndex;
            if (!ReachStart)
            {
                SelectedIndex = SelectedIndex - 1 < 0 ? 0 : SelectedIndex - 1;
                LrcLinePanel.ScrollIntoView(LrcLinePanel.Items[index - 3 > 0 ? index - 3 : SelectedIndex]);
            }
        }

        public void MoveDown()
        {
            int index = SelectedIndex;
            if (!ReachEnd)
            {
                SelectedIndex = SelectedIndex + 1 > LrcLinePanel.Items.Count ? LrcLinePanel.Items.Count : SelectedIndex + 1;
                LrcLinePanel.ScrollIntoView(LrcLinePanel.Items[index + 10 < LrcLinePanel.Items.Count ? index + 10 : SelectedIndex]);
            }
        }
    }
}

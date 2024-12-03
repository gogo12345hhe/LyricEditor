using LyricEditor.Lyric;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace LyricEditor
{
    delegate void CallbackFun(double value);
    /// <summary>
    /// SearchLyric.xaml 的交互逻辑
    /// </summary>
    public partial class SearchLyric : Window
    {
        readonly ObservableCollection<SongInfo> songInfoList = new ObservableCollection<SongInfo>();
        readonly MainWindow mainWindow;

        public SearchLyric(MainWindow _main, string sTitle, string sPerformers)
        {
            InitializeComponent();

            搜索结果List.ItemsSource = songInfoList;

            title.Text = sTitle;
            artist.Text = sPerformers;
            mainWindow = _main;
        }

        readonly Dictionary<string, string> 搜索歌曲 = new Dictionary<string, string>()
        {
            { "网易云音乐", "http://music.163.com/api/search/get/web?csrf_token=hlpretag=&hlposttag=&s={0}&type=1&offset=0&total=true&limit=10" },
            { "QQ音乐", "https://c.y.qq.com/soso/fcgi-bin/client_search_cp?p=1&n=10&w={0}&format=json" }
        };

        readonly Dictionary<string, string> 搜索歌词 = new Dictionary<string, string>()
        {
            { "网易云音乐", "https://music.163.com/api/song/lyric?id={0}&lv=1&kv=1&tv=-1" },
            { "QQ音乐", "https://c.y.qq.com/lyric/fcgi-bin/fcg_query_lyric_new.fcg?songmid={0}&format=json&nobase64=1" }
        };

        private void Serach(string key, Action<double> _Set_SearchPB_Value, Action<SongInfo> _SongInfoList_Add)
        {
            double pbval = 0;
            _Set_SearchPB_Value(pbval += 1);

            foreach (string source in 搜索歌曲.Keys)
            {
                var client = new RestClient();
                var request = new RestRequest(string.Format(搜索歌曲[source], key), Method.Get);
                RestResponse response = client.Execute(request);

                if (source == "网易云音乐")
                {
                    JsonArray dataSongList = JsonSerializer.Deserialize<JsonObject>(response.Content)["result"]["songs"].AsArray();
                    if (dataSongList != null)
                    {
                        foreach (JsonNode song in dataSongList)
                        {
                            string songMid = song["id"].ToString();
                            string songName = song["name"].ToString();
                            if (!songName.Contains(key.Split(' ')[0])) continue;
                            string albumName = song["album"]["name"].ToString();
                            var singer = string.Join(";", song["artists"].AsArray().Select(x => x["name"].ToString()));

                            _SongInfoList_Add(new SongInfo() { SongMid = songMid, SongName = songName, AblumName = albumName, Singer = singer, Source = source });
                        }
                    }
                }
                else if (source == "QQ音乐")
                {
                    JsonArray dataSongList = JsonSerializer.Deserialize<JsonObject>(response.Content)["data"]["song"]["list"].AsArray();
                    foreach (JsonNode song in dataSongList)
                    {
                        string songMid = song["songmid"].ToString();
                        if (songMid == "") continue;
                        string songName = song["songname"].ToString();
                        if (!songName.Contains(key.Split(' ')[0])) continue;
                        string albumName = song["albumname"].ToString();
                        string singer = string.Join(";", song["singer"].AsArray().Select(x => x["name"].ToString()));
                        //if (!singer.Contains(key.Split(' ')[1])) continue;

                        _SongInfoList_Add(new SongInfo() { SongMid = songMid, SongName = songName, AblumName = albumName, Singer = singer, Source = source });
                    }
                }
                _Set_SearchPB_Value(pbval += 1);
            }

            _Set_SearchPB_Value(pbval += 1);
        }

        private string GetLyric(string songmid, string source)
        {
            var client = new RestClient();
            var request = new RestRequest(string.Format(搜索歌词[source], songmid), Method.Get);

            string lyric = "";

            if (source == "网易云音乐")
            {
                RestResponse response = client.Execute(request);

                JsonObject js = JsonSerializer.Deserialize<JsonObject>(response.Content);
                if (js["code"].ToString() == "200")
                {
                    lyric = js["lrc"]["lyric"].ToString();
                }
            }
            else if (source == "QQ音乐")
            {
                request.AddHeader("Referer", "https://y.qq.com/portal/player.html");
                RestResponse response = client.Execute(request);

                JsonObject js = JsonSerializer.Deserialize<JsonObject>(response.Content);
                if (js["code"].ToString() == "0")
                {
                    lyric = js["lyric"].ToString();
                }
            }

            return lyric;
        }

        public async void Search_Click(object sender, RoutedEventArgs e)
        {
            string keyword = $"{title.Text} {artist.Text}";
            if (string.IsNullOrWhiteSpace(keyword)) return;

            songInfoList.Clear();

            void Set_SearchPB_Value(double val) => SearchPB.Dispatcher.Invoke(() => SearchPB.Value = val);

            void SongInfoList_Add(SongInfo info) => Dispatcher.Invoke(() => songInfoList.Add(info));

            try
            {
                await Task.Run(() => Serach(keyword, Set_SearchPB_Value, SongInfoList_Add));
            }
            catch
            {
                e.Handled = true;
                Set_SearchPB_Value(0);
                MessageBox.Show("获取歌曲信息错误");
            }
        }

        private void OnListViewItemDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SongInfo item = 搜索结果List.SelectedItem as SongInfo;

            string lyric = GetLyric(item.SongMid, item.Source);
            LrcManager.Instance.LoadFromText(lyric);

            mainWindow.UpdateLrcViewInvoke();
        }
    }
}


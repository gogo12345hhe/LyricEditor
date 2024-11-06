using System.IO;
using System.Windows.Media.Imaging;

namespace LyricEditor.Utils
{
    public static class TagLibHelper
    {
        ///// <summary>
        ///// 获取音乐文件的封面图
        ///// </summary>
        //public static BitmapImage GetAlbumArt(string filename)
        //{
        //    var file = TagLib.File.Create(filename);
        //    var bin = file.Tag.Pictures[0].Data.Data;
        //    BitmapImage image = new BitmapImage();
        //    image.BeginInit();
        //    image.StreamSource = new MemoryStream(bin);
        //    image.EndInit();
        //    file.Dispose();

        //    return image;
        //}

        ///// <summary>
        ///// 获取音乐文件的歌曲标题
        ///// </summary>
        //public static string GetTitle(string filename)
        //{
        //    var file = TagLib.File.Create(filename);
        //    string title = file.Tag.Title;
        //    file.Dispose();

        //    return title;
        //}

        ///// <summary>
        ///// 获取作者
        ///// </summary>
        ///// <param name="filename"></param>
        ///// <returns></returns>
        //public static string GetPerformers(string filename)
        //{
        //    var file = TagLib.File.Create(filename);
        //    string performers = file.Tag.Performers[0];
        //    file.Dispose();

        //    return performers;
        //}

        ///// <summary>
        ///// 获取唱片集
        ///// </summary>
        ///// <param name="filename"></param>
        ///// <returns></returns>
        //public static string GetAlbum(string filename)
        //{
        //    var file = TagLib.File.Create(filename);
        //    string album = file.Tag.Album;
        //    file.Dispose();

        //    return album;
        //}
    }
}

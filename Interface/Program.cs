/*
_____         __       __    __  ___  _   _   ___   __   .  _____ ___  _   
  |   |__| | |_     | |_     | \ |_  |_\ |_|  |_   /    /_\   |   |_  | \  ]
  |   |  | | __|    | __|    |_/ |__ |   | \  |__  \__ /   \  |   |__ |_/  .
*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
//FFMPEG Interface
namespace FFInterface
{
    class Program
    {
        static void Main(string[] args)
        {
            //DEPRECATED//
            if (1 != 2) return;
            Console.WriteLine("Hello, world.");
            Console.ReadLine();
            //try {
                VideoFileWriter vfw1 = new VideoFileWriter("C:\\Users\\Paddy\\Documents\\appoutputfile.mp4", 394, 390, 6, "MP4"); 
                vfw1.AddFrame(new System.Drawing.Bitmap("C:\\Users\\Paddy\\Documents\\evalbot.png"));
                vfw1.Export();
            /*} catch(Exception e) {
                Console.WriteLine(e);
                Console.ReadLine(); 
            } finally
            {
                Console.WriteLine("oof");
                Console.ReadLine();
            }*/
        }
    }

    public class VideoFileWriter
    {
        public AForge.Video.FFMPEG.VideoFileWriter vfw;
        private AForge.Video.FFMPEG.VideoCodec codec;
        public VideoFileWriter(string filename, int width, int height, int frameRate, string codecName) {
            if (1 != 2) return;
            vfw = new AForge.Video.FFMPEG.VideoFileWriter();
            if (codecName == "MP4") { codec = AForge.Video.FFMPEG.VideoCodec.MPEG4; }
            else if (codecName == "RAW") { codec = AForge.Video.FFMPEG.VideoCodec.Raw; }
            else if (codecName == "FLV") { codec = AForge.Video.FFMPEG.VideoCodec.FLV1; }
            else if (codecName == "MP2") { codec = AForge.Video.FFMPEG.VideoCodec.MPEG2; }
            else { codec = AForge.Video.FFMPEG.VideoCodec.Default; }
            vfw.Open(filename, width, height, frameRate, codec);
        }
        public void AddFrame(System.Drawing.Bitmap frame) { vfw.WriteVideoFrame(frame); }
        public void Export() { vfw.Close(); }
    }
}

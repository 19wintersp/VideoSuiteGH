using AForge.Video.FFMPEG;
using System;
using System.Drawing;
using System.Collections.Generic;

namespace InterForge
{
    public class VFW
    {
        public VFW(System.Drawing.Bitmap bitmap)
        {
            VideoFileWriter exporter = new VideoFileWriter();
            exporter.Open("C:\\Users\\Paddy\\Documents\\outputvideo.mp4", 394, 390);
            exporter.WriteVideoFrame(bitmap);
            exporter.Close();
        }
    }
}
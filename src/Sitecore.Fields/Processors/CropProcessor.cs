using ImageProcessor.Imaging;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Resources.Media;
using System;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Formica.Foundation.Extensions.Pipelines.GetMediaStream
{
    /// <summary>
    /// Utilizes the additional custom focus crop parameters to crop images.
    /// </summary>
    public class CropProcessor
    {
        private static readonly string[] IMAGE_EXTENSIONS = { "bmp", "jpeg", "jpg", "png", "gif" };

        public void Process(GetMediaStreamPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            var outputStream = args.OutputStream;
            if (outputStream == null)
            {
                return;
            }

            if (IMAGE_EXTENSIONS.Any(i => i.Equals(args.MediaData.Extension, StringComparison.InvariantCultureIgnoreCase)))
            {
                var cx = args.Options.CustomOptions["cx"];
                var cy = args.Options.CustomOptions["cy"];
                var width = args.Options.CustomOptions["cw"];
                var height = args.Options.CustomOptions["ch"];

                if (!string.IsNullOrEmpty(cx) && !string.IsNullOrEmpty(cy) && float.TryParse(cx, out float x) && float.TryParse(cy, out float y) &&
                    !string.IsNullOrEmpty(width) && Int32.TryParse(width, out int w) && !string.IsNullOrEmpty(height) && Int32.TryParse(height, out int h))
                {
                    var outputStrm = Stream.Synchronized(GetCroppedImage(args.MediaData.Extension, w, h, x, y, outputStream.MediaItem));
                    args.OutputStream = new MediaStream(outputStrm, args.MediaData.Extension, outputStream.MediaItem);
                }
                else
                {
                    GetThumbnailStream(args);
                }
            }
            else
            {
                GetThumbnailStream(args);
            }
        }

        private Stream GetCroppedImage(string extension, int width, int height, float cx, float cy, MediaItem mediaItem)
        {
            var outputStrm = new MemoryStream();
            var mediaStrm = mediaItem.GetMediaStream();
            var img = Image.FromStream(mediaStrm);
            var proc = new ImageProcessor.ImageFactory();
            proc.Load(img);

            var axis = new float[] { cy, cx };
            proc = proc.Resize(new ResizeLayer(new Size(width, height), ResizeMode.Crop, AnchorPosition.Center, true, centerCoordinates: axis));
            proc.Save(outputStrm);

            return outputStrm;
        }

        private void GetThumbnailStream(GetMediaStreamPipelineArgs args)
        {
            if (args.Options.Thumbnail)
            {
                TransformationOptions transformationOptions = args.Options.GetTransformationOptions();
                MediaStream thumbnailStream = args.MediaData.GetThumbnailStream(transformationOptions);
                if (thumbnailStream != null)
                {
                    args.OutputStream = thumbnailStream;
                }
            }
        }
    }
}

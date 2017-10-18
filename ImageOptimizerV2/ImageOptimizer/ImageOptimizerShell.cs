using System.Text.RegularExpressions;
using Sitecore;
using Sitecore.Configuration;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Resources.Media;
using Sitecore.SecurityModel;
using System;
using System.Diagnostics;
using System.IO;
using ImageMagick;

namespace ImageOptimizerV2.ImageOptimizer
{

    public class ImageOptimizerShell
    {
        protected void AddMessage(string message)
        {
            if (Context.Job != null)
            {
                Context.Job.Status.Messages.Add(message);
            }
        }

        protected virtual bool AttachNewFile(MediaItem item, string filename, string extension)
        {
            if (!File.Exists(filename))
            {
                this.AddMessage("Failed to create output file");
                return false;
            }
            using (Stream stream = File.OpenRead(filename))
            {
                using (new EditContext((Item) item, SecurityCheck.Disable))
                {
                    MediaManager.GetMedia(item).SetStream(stream, extension);
                }
            }
            return true;
        }

        protected virtual string BackupOriginalFile(MediaItem item, string extension)
        {
            string backupFolder = this.BackupFolder;
            
            string path = Path.Combine(backupFolder,
                string.Format("{0:N}_{1:yyyy-MM-dd_hhmmss}.{2}", item.ID.ToGuid(), item.InnerItem.Statistics.Updated, extension));
            if (File.Exists(path))
            {
                return null;
            }
            using (Stream stream = File.OpenWrite(path))
            {
                int num;
                Stream mediaStream = item.GetMediaStream();
                byte[] buffer = new byte[0x2000];
                while ((num = mediaStream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    stream.Write(buffer, 0, num);
                }
            }
            return path;
        }

        protected virtual void Execute(string cmd, string[] args, string src, string dst)
        {
            string str = string.Format(string.Join(" ", args), "\"" + src + "\"", "\"" + dst + "\"");
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo(cmd)
                {
                    Arguments = str,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true
                };
                Process.Start(startInfo).WaitForExit(0x7530);
            }
            catch (Exception exception)
            {
                this.AddMessage("Unable to compress image: " + exception.Message);
                Sitecore.Diagnostics.Log.Error(string.Format("Error compressing image: {0} {1}", cmd, str), exception,
                    this);
            }
            

        }

        protected virtual void OptimizeJpg(string src, string dst)
        {

            string tempFolder = Sitecore.IO.FileUtil.MapPath(Sitecore.Configuration.Settings.TempFolderPath);
            MagickAnyCPU.CacheDirectory = tempFolder;
            MagickNET.SetTempDirectory(tempFolder);

            MagickImage image = new MagickImage(new FileInfo(src));
            image.Quality = this.JpgCompressionQuality;
            image.ColorSpace = ColorSpace.RGB;
            image.Interlace = Interlace.Jpeg;
            image.Strip();

            image.Write(dst, new JpegWriteDefines()
            {
                SamplingFactors = new MagickGeometry[]
               {
                  new MagickGeometry ("2x2"),
                  new MagickGeometry ("1x1"),
                  new MagickGeometry ("1x1")
               }
            });
        }

        protected virtual void OptimizePng(string src, string dst)
        {
            string tempFolder = Sitecore.IO.FileUtil.MapPath(Sitecore.Configuration.Settings.TempFolderPath);
            MagickAnyCPU.CacheDirectory = tempFolder;
            MagickNET.SetTempDirectory(tempFolder);

            FileInfo sourceFile = new FileInfo(src);

            MagickImage image = new MagickImage(new FileInfo(src));
            image.Strip();

            image.Write(dst);

        }

        public void Optimize(MediaItem item)
        {
            try
            {
                string extension = item.Extension.ToLowerInvariant();
                if (((extension != "jpg") && (extension != "jpeg")) && (extension != "png"))
                {
                    this.AddMessage("File is not a jpeg or png image");
                }
                else
                {
                    string src = this.BackupOriginalFile(item, extension);
                    if (src == null)
                    {
                        this.AddMessage(string.Format("{0} alread exists. Skipping compression", item.Name));
                    }
                    else
                    {
                        string dst = string.Empty;
                        try
                        {
                            if (item.MimeType == "image/png")
                            {
                                dst = Regex.Replace(src, @"(.*)\.png$", "$1-or8.png");

                                if (PngUseCommand)
                                    this.Execute(this.PngCommand, this.PngOptions, src, dst);
                                else
                                    OptimizePng(src, dst);
                            }
                            else if ((item.MimeType == "image/jpg") || (item.MimeType == "image/jpeg"))
                            {
                                dst = Regex.Replace(src, @"(.*)\.(jpg|jpeg)$", "$1-out.$2");
                                if (this.JpgUseCommand)
                                    this.Execute(this.JpgCommand, this.JpgOptions, src, dst);
                                else
                                    OptimizeJpg(src, dst);
                            }
                            FileInfo info = new FileInfo(src);
                            FileInfo info2 = new FileInfo(dst);
                            if ((info2.Length > 0L) && (info2.Length < info.Length))
                            {
                                this.AddMessage(string.Format("Reduced file size of {0} from {1} to {2} bytes, {3:0.0}%",
                                    new object[]
                            {
                                item.Name, info.Length, info2.Length,
                                ((info.Length - info2.Length)*100.0)/((double) info.Length)
                            }));
                                this.AttachNewFile(item, dst, extension);
                            }

                            Item it = item;
                            using (new EditContext(it))
                            {
                                it["Image Optimizer Allready Optimized"] = "1";
                            }
                        }
                        finally
                        {
                            try
                            {
                                File.Delete(src);
                            }
                            catch (Exception)
                            {
                                //Do nothing here
                                throw;
                            }

                            try
                            {
                                File.Delete(dst);
                            }
                            catch (Exception)
                            {
                                //Do nothing here
                                throw;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error during the image optimization", ex, this);
                throw;
            }
        }

        protected string BackupFolder
        {
            get
            {
                return MainUtil.MapPath(Settings.TempFolderPath);
            }
        }

        protected bool JpgUseCommand
        {
            get
            {
                bool result = false;
                Boolean.TryParse(Settings.GetSetting("imageOptimizer.jpgUseCommand", "false"), out result);
                return result;
            }
        }

        protected int JpgCompressionQuality
        {
            get
            {
                int result = 85;
                Int32.TryParse(Settings.GetSetting("imageOptimizer.jpgCompressionQuality", "85"), out result);
                return result;
            }
        }

        protected string JpgCommand
        {
            get
            {
                return Settings.GetSetting("imageOptimizer.jpgCommand",
                    string.Format("{0}/jpegtran.exe", Settings.DataFolder));
            }
        }

        protected string[] JpgOptions
        {
            get
            {
                return
                    Settings.GetSetting("imageOptimizer.jpgOptions", "-copy none -progressive -optimize {0} {1}")
                        .Split(new char[] {' '});
            }
        }

        protected bool PngUseCommand
        {
            get
            {
                bool result = false;
                Boolean.TryParse(Settings.GetSetting("imageOptimizer.pngUseCommand", "false"), out result);
                return result;
            }
        }

        protected string PngCommand
        {
            get
            {
                return Settings.GetSetting("imageOptimizer.pngCommand",
                    string.Format("{0}/pngcrush.exe", Settings.DataFolder));
            }
        }

        protected string[] PngOptions
        {
            get
            {
                return
                    Settings.GetSetting("imageOptimizer.pngOptions", "-rem alla -reduce {0} {1}")
                        .Split(new char[] {' '});
            }
        }
    }
}
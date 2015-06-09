using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI.WebControls;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore;
using Sitecore.Data.Query;
using Sitecore.Diagnostics;

namespace ImageOptimizerV2.ImageOptimizer
{
    public class OptimizeImageManager
    {
        public delegate void ImageOptimizedEventHandler(object sender, Item item);
        public event ImageOptimizedEventHandler OnImageOptimized;
        public event ImageOptimizedEventHandler OnImageOptimizing;


        protected static Dictionary<Guid, OptimizeImageManagerStatus> processes = new Dictionary<Guid, OptimizeImageManagerStatus>();

        protected ImageOptimizerShell _shell;
        protected OptimizeImageManagerStatus _status;

        public OptimizeImageManager()
        {
            _shell = new ImageOptimizerShell();
            _status = new OptimizeImageManagerStatus();
        }

        public static OptimizeImageManagerStatus GetStatus(Guid processGuid)
        {
            OptimizeImageManagerStatus status;
            processes.TryGetValue(processGuid, out status);
            return status;
        }

        public Guid Optimize(Database db, string rootItemID, bool thisFolderOnly, bool force)
        {
            ID rootId = ID.Parse(rootItemID);
            Item rootItem = db.GetItem(rootId);
            
            OptimizeImageManagerStatus status = new OptimizeImageManagerStatus();
            status.Status = OptimizeImageManagerStatusEnum.Initializing;

            Guid processGuid = Guid.NewGuid();
            processes.Add(processGuid, status);

            //This part of the method is executed in background
            Task.Factory.StartNew(() => InitOptmizeAsynch(rootItem, thisFolderOnly, force, status));

            return processGuid;
        }

        private void InitOptmizeAsynch(Item rootItem, bool thisFolderOnly, bool force, OptimizeImageManagerStatus status)
        {
            status.Status = OptimizeImageManagerStatusEnum.Running;

            foreach (Item child in rootItem.Children)
            {
                OptmizeAsynch(child, thisFolderOnly, force, status);
            }

            status.Status = OptimizeImageManagerStatusEnum.Finished;
        }

        private void OptmizeAsynch(Item rootItem, bool thisFolderOnly, bool force, OptimizeImageManagerStatus status)
        {
            MediaItem mi = rootItem;
            if((mi.MimeType == "image/jpg") || (mi.MimeType == "image/jpeg") || (mi.MimeType == "image/png"))
            {
                if (force || rootItem["Image Optimizer Allready Optimized"] != "1")
                {
                    _shell.Optimize(mi);
                    status.AlreadyOptimized++;
                }
                else
                {
                    status.Skipped++;
                }
            }

            if (!thisFolderOnly)
            {
                foreach (Item child in rootItem.Children)
                {
                    this.OptmizeAsynch(child, thisFolderOnly, force, status);
                }
            }
        }

        public void Optimize(Sitecore.Data.Items.Item[] itemArray)
        {
            List<Item> filteredItems = new List<Item>();

            foreach (var item in itemArray)
            {
                MediaItem mi = item;
                if((mi.MimeType == "image/jpg") || (mi.MimeType == "image/jpeg") || (mi.MimeType == "image/png"))
                    filteredItems.Add(mi);
            }

            _status.AlreadyOptimized = 0;
            _status.Messages.Add("Running");
            _status.Status = OptimizeImageManagerStatusEnum.Running;

            foreach (var fi in filteredItems)
            {
                OptimizeImage(fi);
                _status.AlreadyOptimized++;
            }
        }

        private void OptimizeImage(Item it)
        {
            Log.Debug(string.Format("Optimizing the image: {0}", it.Paths.FullPath), this);
            OnImageOptimizing(this, it);
            _shell.Optimize(it);
            OnImageOptimized(this, it);
        }
    }
}
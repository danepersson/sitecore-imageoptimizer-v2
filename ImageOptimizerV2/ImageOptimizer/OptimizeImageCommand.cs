using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Jobs;
using Sitecore.Shell.Applications.Dialogs.ProgressBoxes;
using Sitecore.Shell.Framework.Commands;
using System;

namespace ImageOptimizerV2.ImageOptimizer
{
    public class OptimizeImageCommand : Command
    {
        public override void Execute(CommandContext context)
        {
            Assert.ArgumentNotNull(context, "context");
            if (context.Items.Length >= 0)
            {
                ProgressBox.Execute("Image", "Optimize images", new ProgressBoxMethod(this.StartProcess), new object[] { context.Items });
            }
        }

        public override CommandState QueryState(CommandContext context)
        {
            Error.AssertObject(context, "context");
            if (context.Items.Length < 1)
            {
                return CommandState.Disabled;
            }
            try
            {
                Item[] items = context.Items;
                for (int i = 0; i < items.Length; i++)
                {
                    MediaItem item = items[i];
                    if (item == null)
                    {
                        return CommandState.Disabled;
                    }
                    if (item.Size <= 0L)
                    {
                        return CommandState.Disabled;
                    }
                    if (((item.MimeType != "image/jpg") && (item.MimeType != "image/jpeg")) && (item.MimeType != "image/png"))
                    {
                        return CommandState.Disabled;
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Error("Error testing command state", exception, this);
                return CommandState.Disabled;
            }
            return base.QueryState(context);
        }

        public void StartProcess(params object[] parameters)
        {
            Item[] itemArray = (Item[])parameters[0];
            Job job = Context.Job;
            if (job != null)
            {
                job.Status.Total = itemArray.Length;
                job.Status.Processed = 0L;
            }

            var manager = new OptimizeImageManager();
            manager.OnImageOptimized += OnImageOptimized;
            manager.OnImageOptimizing += OnImageOptimizing;
            manager.Optimize(itemArray);
        }

        private void OnImageOptimizing(object sender, Item item)
        {
            Job job = Context.Job;
            if (job != null)
            {
                job.Status.Messages.Add(string.Format("Compressing {0}", item.Name));
            }
        }

        private void OnImageOptimized(object sender, Item item)
        {
            Job job = Context.Job;
            if (job != null)
            {
                JobStatus status = job.Status;
                status.Processed += 1L;
            }
        }
    }
}
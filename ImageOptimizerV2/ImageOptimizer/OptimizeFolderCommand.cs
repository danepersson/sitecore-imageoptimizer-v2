using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;
using Sitecore.Text;
using Sitecore.Web.UI.Sheer;

namespace ImageOptimizerV2.ImageOptimizer
{
    public class OptimizeFolderCommand : Command
    {
        public override void Execute(CommandContext context)
        {
            Assert.ArgumentNotNull(context, "context");
            Item[] items = context.Items;
            if (items.Length == 1)
            {
                UrlString str = new UrlString(UIUtil.GetUri("control:OptimizeFolderDialog"));
                Item item = items[0];
                str["id"] = item.ID.ToString();
                str["la"] = item.Language.ToString();
                str["vs"] = item.Version.ToString();
                SheerResponse.ShowModalDialog(str.ToString());
            }
        }
    }
}
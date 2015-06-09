using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Data.Templates;
using Sitecore.Diagnostics;
using Sitecore.Events;
using Sitecore.Links;
using Sitecore.Publishing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageOptimizerV2.ImageOptimizer
{
    class CompressImageHandler
    {
        protected void OnItemSaved(object sender, EventArgs args)
        {
            if (args == null || LinkDisabler.IsActive || !Settings.LinkDatabase.UpdateDuringPublish && PublishHelper.IsPublishing())
            {
                return;
            }

            Item item = Event.ExtractParameter(args, 0) as Item;

            Assert.IsNotNull(item, "No item in parameters");
            

            var imageTemplateIDs = new ID[] {Sitecore.TemplateIDs.Image, Sitecore.TemplateIDs.UnversionedImage, Sitecore.TemplateIDs.VersionedImage};
            var templateItem = TemplateManager.GetTemplate(item);

            foreach (var templateID in imageTemplateIDs)
            {
                if (IsDerived(templateItem, templateID))
                {
                    var command = new OptimizeImageCommand();
                    command.StartProcess(new object[] { new Item[] { item } });
                    break;
                }
            }
        }

        protected bool IsDerived(Template template, ID templateID)
        {
            return template.ID == templateID || template.GetBaseTemplates().Any(baseTemplate => IsDerived(baseTemplate, templateID));

        }
    }
}
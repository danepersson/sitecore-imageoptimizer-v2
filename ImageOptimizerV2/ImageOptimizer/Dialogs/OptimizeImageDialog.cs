using System.Runtime.InteropServices;
using Sitecore;
using Sitecore.Collections;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Extensions;
using Sitecore.Globalization;
using Sitecore.Jobs;
using Sitecore.Pipelines;
using Sitecore.Publishing;
using Sitecore.Security.AccessControl;
using Sitecore.SecurityModel;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Web.UI;
using System.Web.UI.HtmlControls;

namespace ImageOptimizerV2.ImageOptimizer.Dialogs
{
    public class OptimizeImageDialog : WizardForm
    {
        protected Memo ErrorText;
        protected Literal Status;
        protected Memo ResultText;
        protected Border ShowResultPane;
        protected Border ResultLabel;
        protected Checkbox ForceOptimize;

        public int PoolingInterval
        {
            get { return Settings.GetIntSetting("imageOptimizer.PoolingInterval", 500); }
        }

        protected string ItemID
        {
            get
            {
                return StringUtil.GetString(base.ServerProperties["ItemID"]);
            }
            set
            {
                Assert.ArgumentNotNull(value, "value");
                base.ServerProperties["ItemID"] = value;
            }
        }

        protected override void ActivePageChanged(string page, string oldPage)
        {
            Assert.ArgumentNotNull(page, "page");
            Assert.ArgumentNotNull(oldPage, "oldPage");
            base.NextButton.Header = Translate.Text("Next >");
            if (page == "Settings")
            {
                base.NextButton.Header = "Optimize >";
            }
            base.ActivePageChanged(page, oldPage);
            if (page == "Optimizing")
            {
                base.NextButton.Disabled = true;
                base.BackButton.Disabled = true;
                base.CancelButton.Disabled = true;
                if (Context.ClientPage.ClientRequest.Form["FolderMode"] == "ThisFolderOnly")
                {
                    SheerResponse.SetInnerHtml("OptimizingText", "Optimizing the current folder without the subfolders...");
                }
                else
                {
                    SheerResponse.SetInnerHtml("OptimizingText", "Optimizing the current folder and the subfolders...");
                }
                SheerResponse.SetInnerHtml("OptimizingTarget", "&nbsp;");
                SheerResponse.Timer("StartOptimizer", 10);
            }
        }

        protected override bool ActivePageChanging(string page, ref string newpage)
        {
            Assert.ArgumentNotNull(page, "page");
            Assert.ArgumentNotNull(newpage, "newpage");
            if (page == "Retry")
            {
                newpage = "Settings";
            }
            if (newpage == "Optimizing")
            {

            }
            return base.ActivePageChanging(page, ref newpage);
        }

        public void CheckStatus()
        {
            /*Handle handle = Handle.Parse(this.JobHandle);*/
            if (this.ProcessGuid == Guid.Empty)
            {
                SheerResponse.Timer("CheckStatus", PoolingInterval);
            }
            else
            {
                OptimizeImageManagerStatus status = OptimizeImageManager.GetStatus(this.ProcessGuid);
                if (status == null)
                {
                    throw new Exception("The optimization process was unexpectedly interrupted.");
                }
                if (status.Status == OptimizeImageManagerStatusEnum.Failed)
                {
                    base.Active = "Retry";
                    base.NextButton.Disabled = true;
                    base.BackButton.Disabled = false;
                    base.CancelButton.Disabled = false;
                    this.ErrorText.Value = StringUtil.StringCollectionToString(status.Messages);
                }
                else
                {
                    string str;
                    if (status.Status == OptimizeImageManagerStatusEnum.Running)
                    {
                        str = string.Format("Optimized:{0}<br/><br/>Skipped: {1}", status.AlreadyOptimized, status.Skipped);
                    }
                    else if (status.Status == OptimizeImageManagerStatusEnum.Initializing)
                    {
                        str = Translate.Text("Initializing.");
                    }
                    else
                    {
                        str = Translate.Text("Queued.");
                    }
                    if (status.Status == OptimizeImageManagerStatusEnum.Finished)
                    {
                        this.Status.Text = string.Format("Image processed: {0}.<br/><br/>Image Skipped: {1}", status.AlreadyOptimized, status.Skipped);
                        base.Active = "LastPage";
                        base.BackButton.Disabled = true;
                        string str2 = StringUtil.StringCollectionToString(status.Messages, "\n");
                        if (!string.IsNullOrEmpty(str2))
                        {
                            this.ResultText.Value = str2;
                        }
                    }
                    else
                    {
                        SheerResponse.SetInnerHtml("OptimizingTarget", str);
                        SheerResponse.Timer("CheckStatus", PoolingInterval);
                    }
                }
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
            if (!Context.ClientPage.IsEvent)
            {
                this.ItemID = WebUtil.GetQueryString("id");
            }
        }

        protected void ShowResult()
        {
            this.ShowResultPane.Visible = false;
            this.ResultText.Visible = true;
            this.ResultLabel.Visible = true;
        }

        protected void StartOptimizer()
        {
            string str2;
            bool thisFolderOnly = Context.ClientPage.ClientRequest.Form["FolderMode"] == "ThisFolderOnly";

            this.ProcessGuid = new OptimizeImageManager().Optimize(Context.ContentDatabase, this.ItemID, thisFolderOnly, this.ForceOptimize.Checked);

            SheerResponse.Timer("CheckStatus", PoolingInterval);
        }

        protected Guid ProcessGuid
        {
            get
            {
                Guid guid;
                string s = StringUtil.GetString(base.ServerProperties["ProcessGuid"]);
                if (!Guid.TryParse(s, out guid))
                    guid = Guid.Empty;
                return guid;
            }
            set
            {
                base.ServerProperties["ProcessGuid"] = value;
            }
        }
    }
}
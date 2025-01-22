using Azure.AI.ContentSafety;
using Azure;
using Sitecore.ExperienceForms.Models;
using Sitecore.ExperienceForms.Processing;
using Sitecore.ExperienceForms.Processing.Actions;
using System;
using Sitecore.Configuration;
using System.Linq;

namespace Alyas.Feature.ContentSafety.SubmitActions
{
    public class EnsureContentSafety : SubmitActionBase<string>
    {
        public EnsureContentSafety(ISubmitActionData submitActionData) : base(submitActionData)
        {
        }

        protected override bool Execute(string data, FormSubmitContext formSubmitContext)
        {
            var client = new ContentSafetyClient(new Uri(Settings.GetSetting("Azure.AI.ContentSafety.Endpoint")), new AzureKeyCredential(Settings.GetSetting("Azure.AI.ContentSafety.AccessKey")));
            var content = string.Empty;
            foreach (var field in formSubmitContext.Fields)
            {
                var postedValue = field.GetType().GetProperty("Value")?.GetValue(field);
                if (postedValue == null)
                {
                    continue;
                }

                content += postedValue;
            }

            var analyzeRequest = new AnalyzeTextOptions(content);
            if (!string.IsNullOrEmpty(Settings.GetSetting("Azure.AI.ContentSafety.CustomBlockList")))
            {
                analyzeRequest.BlocklistNames.Add(Settings.GetSetting("Azure.AI.ContentSafety.CustomBlockList"));
                analyzeRequest.HaltOnBlocklistHit = true;
            }
            var analyzeResponse = client.AnalyzeText(analyzeRequest);
            if (analyzeResponse.HasValue && (analyzeResponse.Value.BlocklistsMatch.Any() || analyzeResponse.Value.CategoriesAnalysis.Any(c => c.Severity >= 1)))
            {
                return false;
            }
            return true;
        }
    }
}
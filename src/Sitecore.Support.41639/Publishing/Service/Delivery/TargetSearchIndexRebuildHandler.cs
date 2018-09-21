using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Events;
using Sitecore.Framework.Conditions;
using Sitecore.Publishing.Diagnostics;
using Sitecore.Publishing.Service.Abstractions.Events;
using Sitecore.Publishing.Service.SitecoreAbstractions;

namespace Sitecore.Support.Publishing.Service.Delivery
{
  public class TargetSearchIndexRebuildHandler
  {
    private readonly IContentSearchManager _contentSearchManager;
    private readonly ISearchIndexCustodian _indexCustodian;

    private readonly IList<string> _indexNames;

    public IList<string> IndexNames { get { return _indexNames; } }

    public TargetSearchIndexRebuildHandler() :
        this(new ContentSearchManagerWrapper(), new IndexCustodianWrapper())
    {
    }

    public TargetSearchIndexRebuildHandler(
        IContentSearchManager contentSearchManager,
        ISearchIndexCustodian indexCustodian)
    {
      Condition.Requires(contentSearchManager, "contentSearchManager").IsNotNull();
      Condition.Requires(indexCustodian, "indexCustodian").IsNotNull();

      _contentSearchManager = contentSearchManager;
      _indexCustodian = indexCustodian;
      _indexNames = new List<string>();
    }

    public void RebuildTargetSearchIndex(object sender, EventArgs args)
    {
      Condition.Requires(sender, "sender").IsNotNull();
      Condition.Requires(args, "args").IsNotNull();

      var sitecoreEventArgs = args as SitecoreEventArgs;

      if ((sitecoreEventArgs != null ? sitecoreEventArgs.Parameters : null) == null ||
          !sitecoreEventArgs.Parameters.Any() ||
          !(sitecoreEventArgs.Parameters[0] is TargetSearchIndexRebuildEventArgs))
      {
        // TODO: Use better logging
        Sitecore.Diagnostics.Log.Error("Attempted to raise the remote item events at the end of a bulk publish, but the event arguments were not valid.", this);
        return;
      }

      // TODO: Run these jobs in parallel and aggregate the wait function.
      foreach (var indexName in IndexNames)
      {
        var searchIndex = _contentSearchManager.GetIndex(indexName);

        PublishingLog.Info("Starting search index rebuild job for " + indexName);

        var job = _indexCustodian.FullRebuild(searchIndex, true);
        job.Wait();

        PublishingLog.Info("Finished search index rebuild job for " + indexName);
      }
    }

    public void AddIndex(string indexName)
    {
      _indexNames.Add(indexName);
    }
  }
}
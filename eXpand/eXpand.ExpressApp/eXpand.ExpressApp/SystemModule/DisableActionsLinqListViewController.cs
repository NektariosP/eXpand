﻿using DevExpress.ExpressApp;
using DevExpress.ExpressApp.SystemModule;
using eXpand.ExpressApp.Core;

namespace eXpand.ExpressApp.SystemModule
{
    public class DisableActionsLinqListViewController : ViewController<ListView> {
        private const string DefaultReason = "LinqListViewController is active";
        protected override void OnActivated()
        {
            base.OnActivated();
            bool flag = !View.Id.EndsWith(LinqCollectionSource.DefaultSuffix);
            Frame.GetController<ListViewProcessCurrentObjectController>().Active[DefaultReason] = flag;
            Frame.GetController<DeleteObjectsViewController>().Active[DefaultReason] = flag;
            Frame.GetController<NewObjectViewController>().Active[DefaultReason] = flag;
            Frame.GetController<FilterController>().Active[DefaultReason] = flag;
        }
        protected override void OnDeactivating()
        {
            base.OnDeactivating();
            Frame.GetController<ListViewProcessCurrentObjectController>().Active.RemoveItem(DefaultReason);
            Frame.GetController<DeleteObjectsViewController>().Active.RemoveItem(DefaultReason);
            Frame.GetController<NewObjectViewController>().Active.RemoveItem(DefaultReason);
            Frame.GetController<FilterController>().Active.RemoveItem(DefaultReason);
        }
    }
}
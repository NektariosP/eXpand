using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Model.Core;
using DevExpress.ExpressApp.Utils;
using DevExpress.ExpressApp.Validation;
using DevExpress.Persistent.Validation;

namespace Xpand.ExpressApp.Validation {
    public interface IModelRuleBaseRuleType : IModelNode {
        [Category("eXpand")]
        RuleType RuleType { get; set; }
    }

    public class RuleTypeController : ViewController<ObjectView>, IModelExtender {
        public const string ObjectSpaceObjectChanged = "ObjectSpaceObjectChanged";
        protected Dictionary<RuleType, IEnumerable<RuleSetValidationResultItem>> Dictionary = new Dictionary<RuleType, IEnumerable<RuleSetValidationResultItem>>();
        protected List<Dictionary<ColumnWrapper, RuleType>> Columns = new List<Dictionary<ColumnWrapper, RuleType>>();

        protected override void OnActivated() {
            base.OnActivated();
            Validator.RuleSet.ValidationCompleted += RuleSetOnValidationCompleted;
            if (HasNonCriticalRulesForControlValueChangedContext()) {
                ObjectSpace.ObjectChanged += ObjectSpaceOnObjectChanged;
                ObjectSpace.Disposed += ObjectSpaceOnDisposed;
            }
        }

        void ObjectSpaceOnDisposed(object sender, EventArgs eventArgs) {
            var objectSpace = ((IObjectSpace)sender);
            objectSpace.Disposed -= ObjectSpaceOnDisposed;
            objectSpace.ObjectChanged -= ObjectSpaceOnObjectChanged;
        }

        void ObjectSpaceOnObjectChanged(object sender, ObjectChangedEventArgs objectChangedEventArgs) {
            if (!string.IsNullOrEmpty(objectChangedEventArgs.PropertyName)) {
                ValidateControlValueChangedContext(objectChangedEventArgs.Object);
            }
        }

        protected bool HasNonCriticalRulesForControlValueChangedContext() {
            return ((IModelApplicationValidation)Application.Model).Validation.Rules.OfType<IModelRuleBaseRuleType>().FirstOrDefault(IsTypeInfoCriticalRuleForControlValueChangedContext()) != null;
        }

        Func<IModelRuleBaseRuleType, bool> IsTypeInfoCriticalRuleForControlValueChangedContext() {
            return type => type.RuleType != RuleType.Critical && ((IRuleBaseProperties)type).TargetType == View.ObjectTypeInfo.Type && ((IRuleBaseProperties)type).TargetContextIDs.Contains(ObjectSpaceObjectChanged);
        }

        protected void ValidateControlValueChangedContext(object currentObject) {
            Validator.RuleSet.ValidateAll(new List<object> { currentObject }, ObjectSpaceObjectChanged);
        }

        protected override void OnDeactivated() {
            base.OnDeactivated();
            if (Validator.RuleSet != null)
                Validator.RuleSet.ValidationCompleted -= RuleSetOnValidationCompleted;
        }

        void RuleSetOnValidationCompleted(object sender, ValidationCompletedEventArgs validationCompletedEventArgs) {
            Columns = new List<Dictionary<ColumnWrapper, RuleType>>();
            if (View == null || View.IsDisposed)
                return;
            if (!validationCompletedEventArgs.Successful) {
                var items = new Dictionary<RuleType, List<RuleSetValidationResultItem>>();
                var ruleTypes = CaptionHelper.GetLocalizedItems("Enums/" + typeof(RuleType).FullName).Select(pair => (RuleType)Enum.Parse(typeof(RuleType), pair.Key)).OrderByDescending(type => type);
                foreach (var ruleType in ruleTypes) {
                    var resultsPerType = GetResultsPerType(validationCompletedEventArgs, ruleType);
                    items.Add(ruleType, resultsPerType);
                    Collect(resultsPerType, ruleType);
                }
                validationCompletedEventArgs.Handled = CriticalErrorsNotExist(items);
            }
        }

        bool CriticalErrorsNotExist(Dictionary<RuleType, List<RuleSetValidationResultItem>> items) {
            return items.FirstOrDefault(pair => pair.Key == RuleType.Critical).Value.Count == 0;
        }

        void Collect(IEnumerable<RuleSetValidationResultItem> resultItems, RuleType ruleType) {
            Dictionary[ruleType] = resultItems;
            if (View is DetailView)
                CollectPropertyEditors(resultItems, ruleType);
            var listView = View as ListView;
            if (listView != null && listView.AllowEdit && ListEditor != null) {
                CollectColumns(resultItems, ruleType);
            }
        }
        public ColumnsListEditor ListEditor {
            get { return View is ListView ? ((ListView)View).Editor as ColumnsListEditor : null; }
        }

        void CollectColumns(IEnumerable<RuleSetValidationResultItem> resultItems, RuleType ruleType) {
            Dictionary<ColumnWrapper, RuleType> ruleTypes = resultItems.SelectMany(GetColumns).Distinct().ToDictionary(wrapper => wrapper, wrapper => ruleType);
            Columns.Add(ruleTypes);
        }

        IEnumerable<ColumnWrapper> GetColumns(RuleSetValidationResultItem resultItem) {
            return ListEditor.Columns.Where(wrapper => resultItem.Rule.UsedProperties.Contains(wrapper.PropertyName));
        }

        List<RuleSetValidationResultItem> GetResultsPerType(ValidationCompletedEventArgs validationCompletedEventArgs, RuleType ruleType) {
            return validationCompletedEventArgs.Exception.Result.Results.Where(item => item.State == ValidationState.Invalid && IsOfRuleType(item, ruleType)).ToList();
        }

        protected virtual Dictionary<PropertyEditor, RuleType> CollectPropertyEditors(IEnumerable<RuleSetValidationResultItem> result, RuleType ruleType) {
            var propertyEditors = result.SelectMany(GetPropertyEditors).Distinct();
            return propertyEditors.ToDictionary(propertyEditor => propertyEditor, propertyEditor => ruleType);
        }


        IEnumerable<PropertyEditor> GetPropertyEditors(RuleSetValidationResultItem resultItem) {
            return View.GetItems<PropertyEditor>().Where(editor => resultItem.Rule.UsedProperties.Contains(editor.MemberInfo.Name) && editor.Control != null);
        }

        bool IsOfRuleType(RuleSetValidationResultItem resultItem, RuleType ruleType) {
            IModelValidationRules modelValidationRules = ((IModelApplicationValidation)Application.Model).Validation.Rules;
            var modelRuleBaseWarning = ((IModelRuleBaseRuleType)modelValidationRules.OfType<ModelNode>().SingleOrDefault(node => node.Id == resultItem.Rule.Id));
            return (modelRuleBaseWarning == null && ruleType == RuleType.Critical) || (modelRuleBaseWarning != null && modelRuleBaseWarning.RuleType == ruleType);
        }

        public void ExtendModelInterfaces(ModelInterfaceExtenders extenders) {
            extenders.Add<IModelRuleBase, IModelRuleBaseRuleType>();
        }
    }

}
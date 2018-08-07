using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Automation;

namespace USG_Access_Service
{
    class AutomationHandler
    {
        // Constants.
        private const string _USCAN_WINDOW_NAME = "uScan2 GUI";

        // Private fields.
        private AutomationElement _uScanWindow;
        private AutomationElement _gainValueElement;
        private AutomationElement _consoleElement;
        private InvokePattern _consoleButton;
        private AutomationElement _txFrequencyElement;
        private AutomationElement _txTypeElement;
        private AutomationElement _imagingRangeElement;
        private AutomationElement _framerateElement;
        private AutomationElement _paletteElement;
        private AutomationElement _txSginalElement;
        private InvokePattern _freezeButton;
        private InvokePattern _gainUpButton;
        private InvokePattern _gainDownButton;
        private InvokePattern _areaUpButton;
        private InvokePattern _areaDownButton;
        private Dictionary<string, SelectionItemPattern> _paletteElements;
        private Dictionary<string, SelectionItemPattern> _txSginalElements;

        // Properties.
        internal bool IsConsoleVisible => _consoleElement.Current.Name == "<<<";
        internal string GainValue => _gainValueElement.Current.Name;
        internal string TxFrequencyValue => _txFrequencyElement.Current.Name;
        internal string TxTypeValue => _txTypeElement.Current.Name;
        internal string ImagingRangeValue => _imagingRangeElement.Current.Name;
        internal string FramerateValue => _framerateElement.Current.Name;
        internal string PaletteValue => _paletteElement.Current.Name;
        internal string TxSignalValue => _txSginalElement.Current.Name;

        // Methods
        public AutomationHandler()
        {
            Initialize();
        }

        internal void Initialize()
        {
            AutomationElement rootDesktopAutomationElement = AutomationElement.RootElement;
            Condition windowNameCondition = new PropertyCondition(AutomationElement.NameProperty, _USCAN_WINDOW_NAME);
            _uScanWindow = rootDesktopAutomationElement.FindFirst(TreeScope.Children, windowNameCondition);
            while (_uScanWindow is null)
            {
                Console.WriteLine("Waiting for uScan window: " + _USCAN_WINDOW_NAME);
                Thread.Sleep(1000);
                _uScanWindow = rootDesktopAutomationElement.FindFirst(TreeScope.Children, windowNameCondition);
            }
            _uScanWindow.SetFocus();

            _consoleElement = GetElement("Hide_Btn");
            _consoleButton = GetInvokePattern(_consoleElement);
            var wasShown = IsConsoleVisible;
            ShowConsole();
            _freezeButton = GetInvokePattern(GetElement("Freeze_Btn"));
            _gainValueElement = GetElement("gain_disp");
            _gainUpButton = GetInvokePattern(GetElement("gain_up"));
            _gainDownButton = GetInvokePattern(GetElement("gain_down"));
            _txFrequencyElement = GetElement("TXFreqLabel");
            _txTypeElement = GetElement("TXTypeLabel");
            _imagingRangeElement = GetElement("area_disp");
            _areaUpButton = GetInvokePattern(GetElement("area_up"));
            _areaDownButton = GetInvokePattern(GetElement("area_down"));
            _framerateElement = GetElement("FramerateLabel");
            _paletteElement = GetElement("Palettecombo");
            _paletteElements = GetElements(_paletteElement);
            _txSginalElement = GetElement("TXcombo");
            _txSginalElements = GetElements(_txSginalElement);
            if (wasShown)
            {
                ShowConsole();
            }
            else
            {
                HideConsole();
            }
            Console.WriteLine("uScan automation initialized.");
        }

        private Dictionary<string, SelectionItemPattern> GetElements(AutomationElement element)
        {
            var elements = new Dictionary<string, SelectionItemPattern>();
            try
            {
                ExpandCollapsePattern expandCollapsePattern = element.GetCurrentPattern(ExpandCollapsePattern.Pattern) as ExpandCollapsePattern
                    ?? throw new ApplicationException($"Couldn't get ExpandCollapse Pattern for combo {element.Current.AutomationId}");
                expandCollapsePattern.Expand();
                if (expandCollapsePattern.Current.ExpandCollapseState != ExpandCollapseState.Expanded)
                {
                    throw new ApplicationException($"Couldn't expand {element.Current.AutomationId}");
                }

                var treeWalker = TreeWalker.ControlViewWalker;
                Console.WriteLine("Children:");
                var child = treeWalker.GetFirstChild(element);
                while (child != null)
                {
                    Console.WriteLine($"{child.Current.ControlType.LocalizedControlType} - [{child.Current.Name}]");
                    AutomationPattern automationPatternFromElement =
                        GetSpecifiedPattern(child, "SelectionItemPatternIdentifiers.Pattern")
                        ?? throw new ApplicationException($"Couldn't get AutomationPattern for list item.");
                    SelectionItemPattern selectionItemPattern =
                        child.GetCurrentPattern(automationPatternFromElement) as SelectionItemPattern
                        ?? throw new ApplicationException($"Couldn't get SelectionItemPattern.");
                    elements[child.Current.Name] = selectionItemPattern;

                    child = treeWalker.GetNextSibling(child);
                }
                expandCollapsePattern.Collapse();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Couldn't find descendants for {element.Current.AutomationId}. Reason: {ex.Message}");
            }

            return elements;
        }

        public AutomationElement GetElement(string elementName)
        {
            Condition propertyCondition = new PropertyCondition(AutomationElement.AutomationIdProperty, elementName);
            if (_uScanWindow.FindFirst(TreeScope.Descendants, propertyCondition) is AutomationElement element)
            {
                return element;
            }
            throw new ApplicationException("Couldn't find element: " + elementName);
        }

        private InvokePattern GetInvokePattern(AutomationElement element)
        {
            if (element.GetCurrentPattern(InvokePattern.Pattern) is InvokePattern invokePattern)
            {
                return invokePattern;
            }
            throw new ApplicationException("Couldn't get invoke pattern.");
        }

        internal void ShowConsole()
        {
            if (!IsConsoleVisible)
            {
                _consoleButton.Invoke();
            }
        }

        internal void HideConsole()
        {
            if (IsConsoleVisible)
            {
                _consoleButton.Invoke();
            }
        }

        internal void Freeze() => _freezeButton.Invoke();
        internal void GainUp() => _gainUpButton.Invoke();
        internal void GainDown() => _gainDownButton.Invoke();
        internal void AreaUp() => _areaUpButton.Invoke();
        internal void AreaDown() => _areaDownButton.Invoke();

        /*
        // Private methods
        private void SelectFromCombo(AutomationElement element, string selection)
        {
            try
            {
                ExpandCollapsePattern expandCollapsePattern = element.GetCurrentPattern(ExpandCollapsePattern.Pattern) as ExpandCollapsePattern
                    ?? throw new ApplicationException($"Couldn't get ExpandCollapse Pattern for combo {element.Current.AutomationId}");
                expandCollapsePattern.Expand();
                if (expandCollapsePattern.Current.ExpandCollapseState != ExpandCollapseState.Expanded)
                {
                    throw new ApplicationException($"Couldn't expand {element.Current.AutomationId}");
                }

                var treeWalker = TreeWalker.ControlViewWalker;
                Console.WriteLine("Children:");
                var child = treeWalker.GetFirstChild(element);
                while (child != null)
                {
                    Console.WriteLine($"{child.Current.ControlType.LocalizedControlType} - [{child.Current.Name}]");
                    child = treeWalker.GetNextSibling(child);
                }

                expandCollapsePattern.Collapse();
                AutomationElement listItem = 
                    element.FindFirst(TreeScope.Subtree, new PropertyCondition(AutomationElement.NameProperty, selection))
                    ?? throw new ApplicationException($"Couldn't find item '{selection}' in combo descendants");
                AutomationPattern automationPatternFromElement = 
                    GetSpecifiedPattern(listItem, "SelectionItemPatternIdentifiers.Pattern")
                    ?? throw new ApplicationException($"Couldn't get AutomationPattern for lit item.");
                SelectionItemPattern selectionItemPattern = 
                    listItem.GetCurrentPattern(automationPatternFromElement) as SelectionItemPattern
                    ?? throw new ApplicationException($"Couldn't get SelectionItemPattern.");
                selectionItemPattern.Select();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Couldn't select {element.Current.Name} -> {selection}. Reason: {ex.Message}");
            }
        }
        */

        internal void PaletteChange(string name)
        {
            _paletteElements[name].Select();
        }

        internal void SignalChange(string name)
        {
            _txSginalElements[" " + name].Select(); // These elements have additional space character on the beginning.
        }

        private static AutomationPattern GetSpecifiedPattern(AutomationElement element, string patternName)
        {
            AutomationPattern[] supportedPatterns = element.GetSupportedPatterns();
            Console.WriteLine("Supported patterns:");
            foreach (var automationPattern in supportedPatterns)
            {
                Console.WriteLine($"  [{automationPattern.ProgrammaticName}]");
            }

            return supportedPatterns.FirstOrDefault(pattern => pattern.ProgrammaticName == patternName);
        }
    }
}

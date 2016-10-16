using System;
using System.Collections.Generic;
using System.Globalization;
using NetworkSkins.UI;
using UnityEngine;

namespace NetworkSkins.Props
{
    public class UICatenaryOption : UIDropDownOption
    {
        private List<PropInfo> _availableCatenaries;

        protected override void Initialize() 
        {
            Description = "Catenary";
            base.Initialize();
        }
        
        protected override bool PopulateDropDown()
        {
            _availableCatenaries = PropCustomizer.Instance.GetAvailableCatenaries(SelectedPrefab);

            if (SelectedPrefab != null && _availableCatenaries != null && PropCustomizer.Instance.HasCatenaries(SelectedPrefab) > 0)
            {
                var defaultProp = PropCustomizer.Instance.GetDefaultCatenary(SelectedPrefab);
                var activeProp = PropCustomizer.Instance.GetActiveCatenary(SelectedPrefab);

                DropDown.items = new string[0];

                foreach (var prop in _availableCatenaries)
                {
                    var itemName = UIUtil.GenerateBeautifiedPrefabName(prop);
                    itemName = BeautifyNameEvenMore(itemName);
                    if (prop == defaultProp) itemName += " (Default)";

                    DropDown.AddItem(itemName);

                    if (prop == activeProp) DropDown.selectedIndex = DropDown.items.Length - 1;
                }

                if (_availableCatenaries.Count >= 2)
                {
                    DropDown.Enable();
                    return true;
                }
                else
                {
                    DropDown.Disable();
                    return false;
                }
            }
            return false;
        }

        private string BeautifyNameEvenMore(string itemName)
        {
            switch (itemName)
            {
                default: return itemName;
            }
        }

        protected override void OnSelectionChanged(int index)
        {
            PropCustomizer.Instance.SetCatenary(SelectedPrefab, _availableCatenaries[index]);
        }
    }
}

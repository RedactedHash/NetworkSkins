using NetworkSkins.UI;
using System;
using System.Collections.Generic;

namespace NetworkSkins.Ground
{
    public class UIGroundOption : UIDropDownOption
    {
        private List<GroundType> _availableGroundTypes;

        protected override void Initialize()
        {
            Description = "Ground Type";
            base.Initialize();
        }

        protected override bool PopulateDropDown()
        {
            _availableGroundTypes = GroundCustomizer.Instance.GetAvailableGroundTypes(SelectedPrefab);

            if (_availableGroundTypes != null && _availableGroundTypes.Count >= 2 
                && SelectedPrefab != null && SelectedPrefab.m_followTerrain 
                && (SelectedPrefab.m_createRuining || SelectedPrefab.m_createGravel || SelectedPrefab.m_createPavement))
            { 
                var defaultGround = GroundCustomizer.Instance.GetDefaultGroundType(SelectedPrefab);
                var activeGround = GroundCustomizer.Instance.GetActiveGroundType(SelectedPrefab);

                DropDown.items = new string[0];

                foreach (var groundType in _availableGroundTypes)
                {
                    var itemName = groundType.ToString();
                    if (groundType == defaultGround) itemName += " (Default)";
                    DropDown.AddItem(itemName);

                    if (groundType == activeGround) DropDown.selectedIndex = DropDown.items.Length - 1;
                }
                return true;
            }
            return false;
        }

        protected override void OnSelectionChanged(int index)
        {
            GroundCustomizer.Instance.SetGroundType(SelectedPrefab, _availableGroundTypes[index]);
        }
    }
}

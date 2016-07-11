using System;
using System.Collections.Generic;
using System.Globalization;
using NetworkSkins.Props;
using NetworkSkins.UI;
using UnityEngine;

namespace NetworkSkins.Skins
{
    public class UISkinOption : UIDropDownOption
    {
        private List<SkinsDefinition.Skin> _availableSkins;

        protected override void Initialize() 
        {
            Description = "Skin";
            base.Initialize();
        }
        
        protected override bool PopulateDropDown()
        {
            _availableSkins = SkinManager.Instance.GetAvailableSkins(SelectedPrefab);

            if (SelectedPrefab != null && _availableSkins != null)
            {
                var activeSkin = SkinManager.Instance.GetActiveSkin(SelectedPrefab);

                DropDown.items = new string[0];

                DropDown.AddItem("Default");
                DropDown.selectedIndex = 0;

                foreach (var skin in _availableSkins)
                {
                    var itemName = skin.DisplayName;

                    DropDown.AddItem(skin.DisplayName);

                    if (skin == activeSkin) DropDown.selectedIndex = DropDown.items.Length - 1;
                }

                if (_availableSkins.Count >= -1) // TODO testing
                {
                    DropDown.Enable();
                }
                else
                {
                    DropDown.Disable();
                }
                return true;
            }
            return false;
        }

        protected override void OnSelectionChanged(int index)
        {
            SkinManager.Instance.SetSkin(SelectedPrefab, index == 0 ? null : _availableSkins[index - 1]);
        }
    }
}

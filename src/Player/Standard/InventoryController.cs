using System.Collections.Generic;
using Godot;

namespace MSSC2027.Player.Standard;

public partial class InventoryController : MeshInstance3D
{
    #region Zmienne
    // Eksporty
    [ExportCategory("Referencje")]
    [Export] private string _blend2BlendPath;
    [Export] private AnimationTree _animTree;
    [Export] private Node3D _handNode;

    // Ekwipunek
    public class ItemData
    {
        public MeshInstance3D InventoryMesh;
        public MeshInstance3D HandMesh;
        public bool IsEquipped = false;
    }
    [System.NonSerialized] private List<ItemData> _inventoryItems = new();

    // Animacje
    private AnimationNodeBlend2 _blend2;

    // Zmienne logiczne
    private bool _equipped = false;
    #endregion

    #region Funkcje Lokalne

    #endregion

    public override void _Ready()
    {
        foreach (Node child in GetChildren())
        {
            MeshInstance3D invMesh = child as MeshInstance3D ?? child.GetNodeOrNull<MeshInstance3D>("MeshInstance3D");

            if (invMesh != null && _handNode != null)
            {
                string handNodeName = child.Name + "Hand";

                if (_handNode.FindChild(handNodeName, true, false) is MeshInstance3D handMesh)
                {
                    _inventoryItems.Add(new ItemData
                    {
                        InventoryMesh = invMesh,
                        HandMesh = handMesh,
                        IsEquipped = false
                    });
                }
            }
        }

        foreach (var item in _inventoryItems)
        {
            string itemName = item.InventoryMesh != null ? item.InventoryMesh.Name : "Brak mesha";
            GD.Print($"Przedmiot: {itemName} | Zaekwipowany: {item.IsEquipped}");
        }
    }

    public void ToggleInventory(bool isActive)
    {
        if (isActive)
        {
            Visible = true;
            Input.MouseMode = Input.MouseModeEnum.Visible;
        }
        else
        {
            Visible = false;
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
    }

    public void EquipItem()
    {
        _equipped = true;
        _animTree.Set(_blend2BlendPath, 1f);
    }

    public void UnequipItem()
    {
        _equipped = false;
        _animTree.Set(_blend2BlendPath, 0f);
    }
}

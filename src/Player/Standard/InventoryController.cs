using System.Collections.Generic;
using Godot;

namespace MSSC2027.Player.Standard;

public partial class InventoryController : MeshInstance3D
{
    #region Zmienne
    // Eksporty
    [ExportCategory("Referencje")]
    [Export] private Node3D _handNode;

    // Ekwipunek
    public class ItemData
    {
        public Area3D ItemArea;
        public MeshInstance3D InventoryMesh;
        public MeshInstance3D HandMesh;
        public bool IsEquipped = false;
    }
    private List<ItemData> _inventoryItems = new();
    private ItemData _currentlyHoveredItem = null;

    // Zmienne logiczne
    private bool _equipped = false;

    // Bazowe wratości
    private const float _heightDifference = 0.44f;
    private Vector3 _baseInventoryPosition;
    #endregion

    #region Funkcje Lokalne
    private static void HighlightItem(MeshInstance3D meshMesh, bool enable)
    {
        if (enable)
        {
            meshMesh.Scale = new Vector3(1.2f, 1.2f, 1.2f);
        }
        else
        {
            meshMesh.Scale = new Vector3(1f, 1f, 1f);
        }
    }

    private void OnItemMouseEntered(ItemData item)
    {
        if (!Visible) return;

        HighlightItem(item.InventoryMesh, true);
        _currentlyHoveredItem = item;
    }

    private void OnItemMouseExited(ItemData item)
    {
        if (_currentlyHoveredItem == item)
        {
            HighlightItem(item.InventoryMesh, false);
            _currentlyHoveredItem = null;
        }
    }
    #endregion

    public override void _Ready()
    {
        // Ustawianie bazowych wartości
        _baseInventoryPosition = Position;

        // Ekwipunek
        foreach (Node child in GetChildren())
        {
            MeshInstance3D invMesh = child as MeshInstance3D ?? child.GetNodeOrNull<MeshInstance3D>("MeshInstance3D");

            if (invMesh != null && _handNode != null)
            {
                Area3D itemArea = invMesh.GetNode<Area3D>("ItemArea"); // W itemach musi być area3d o nazwie "ItemArea"
                string handNodeName = child.Name + "Hand";

                if (_handNode.FindChild(handNodeName, true, false) is MeshInstance3D handMesh)
                {
                    var newItem = new ItemData
                    {
                        ItemArea = itemArea,
                        InventoryMesh = invMesh,
                        HandMesh = handMesh,
                        IsEquipped = false
                    };

                    _inventoryItems.Add(newItem);

                    // Podpięcie sygnałów
                    itemArea.MouseEntered += () => OnItemMouseEntered(newItem);
                    itemArea.MouseExited += () => OnItemMouseExited(newItem);
                }
            }
        }

        // Print wartości
        foreach (ItemData item in _inventoryItems)
        {
            string itemName = item.InventoryMesh != null ? item.InventoryMesh.Name : "Brak mesha";
            GD.Print($"Przedmiot: {itemName} | Hand Mesh: {item.HandMesh.Name} | Item Area: {item.ItemArea.Name} | Equipped: {item.IsEquipped}");
        }
    }

    public void ToggleInventory(bool isOpen, bool isCrouching)
    {
        if (isOpen)
        {
            Visible = true;
            Input.MouseMode = Input.MouseModeEnum.Visible;
            if (isCrouching)
            {
                Vector3 currentPos = Position;
                currentPos.Y = _baseInventoryPosition.Y - _heightDifference;
                Position = currentPos;
            }
            else
            {
                Position = _baseInventoryPosition;
            }
        }
        else
        {
            Visible = false;
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
    }

    public void TryInteractWithHoveredItem()
    {
        if (_currentlyHoveredItem == null || !Visible) return;

        _currentlyHoveredItem.IsEquipped = !_currentlyHoveredItem.IsEquipped;

        if (_currentlyHoveredItem.IsEquipped)
        {
            _currentlyHoveredItem.InventoryMesh.Visible = false;
            _currentlyHoveredItem.HandMesh.Visible = true;
            _equipped = true;
        }
        else
        {
            _currentlyHoveredItem.InventoryMesh.Visible = true;
            _currentlyHoveredItem.HandMesh.Visible = false;
            _equipped = false;
        }
    }
}

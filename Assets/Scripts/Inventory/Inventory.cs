﻿using NaughtyAttributes;
using OneLine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class Inventory : MonoBehaviour
{
	[OneLineWithHeader] [HideLabel] public List<InventoryItemSlot> Items;

	[SerializeField] [Min(0)] private float ItemPickUpRange = 3;
	[SerializeField] [Min(0)] private float ItemPickUpVerticalExtend = 1;

	[SerializeField] [Foldout("Gizmos")] private Color RangeGizmoColor = Color.green;
	[SerializeField] [Foldout("Gizmos")] private Color DetectedItemsGizmoColor = Color.blue;
	[SerializeField] [Foldout("Gizmos")] private Color HoveredItemGizmoColor = Color.red;

	private List<WorldItem> ItemsInRange = new List<WorldItem>();

	private void Start()
	{
		CleanInventory();//Items.ForEach(I => Debug.Log(I.Item.gameObject.scene.name));
		MouseHoverMonitor.Inst.OnMouseClickGameObject.AddListener(OnClick);
	}

	public void CleanInventory()
	{
		//Remove NULL or Invalid Count Entries
		Items.RemoveAll(I => I.Item == null || I.Count < 1);
		//Merge Duplicate Item Entries
		var GroupedItems = Items.GroupBy(I => I.Item);
		foreach (var item in GroupedItems)
		{
			if (item.Count() > 1)
			{
				int FirstIndex = Items.FindIndex(I => I.Item == item.Key);
				for (int i = 1 ; i < item.Count() ; i++)
				{
					Items[FirstIndex].Count += item.ElementAt(i).Count;
					int Index = Items.FindIndex(I => I == item.ElementAt(i));
					Items.RemoveAt(Index);
				}
			}
		}
	}

	private void LateUpdate()
	{
		var _ItemsInRange = Physics.OverlapCapsule(transform.position + ItemPickUpVerticalExtend * Vector3.up,
						 transform.position + ItemPickUpVerticalExtend * Vector3.down,
						 ItemPickUpRange).ToList().ConvertAll(Coll => Coll.transform.parent?.GetComponent<WorldItem>() ?? null);
		_ItemsInRange.RemoveAll(I => I == null);
		ItemsInRange.RemoveAll(I => !_ItemsInRange.Contains(I));
		_ItemsInRange.ForEach(I => { if (!ItemsInRange.Contains(I)) { ItemsInRange.Add(I); } });
	}

	private void OnClick(GameObject GO)
	{
		WorldItem WI = GO.GetComponentInParent<WorldItem>();
		if (WI != null && ItemsInRange.Contains(WI))
		{
			PickUpItem(WI);
		}
	}

	public void PickUpItem(WorldItem Item, int Count = 1)
	{
		Items.Add(new InventoryItemSlot(Item.Item, Count));
		Item.gameObject.SetActive(false);//Destroy??
		CleanInventory();
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = RangeGizmoColor;
		Gizmos.DrawWireSphere(transform.position + ItemPickUpVerticalExtend * Vector3.up, ItemPickUpRange);
		Gizmos.DrawWireSphere(transform.position + ItemPickUpVerticalExtend * Vector3.down, ItemPickUpRange);

		if (Application.isPlaying && ItemsInRange.Count > 0)
		{
			ItemsInRange.ForEach(I =>
			{
				Gizmos.color = (MouseHoverMonitor.Inst.GameObject?.transform.parent?.gameObject != I.gameObject) ? DetectedItemsGizmoColor : HoveredItemGizmoColor;
				Gizmos.DrawLine(transform.position, I.transform.position);
			});
		}
	}
}

[System.Serializable]
public class InventoryItemSlot
{
	[Weight(4)] public InventoryItem Item;
	[Min(1)] public int Count = 1;

	public InventoryItemSlot(InventoryItem item, int count = 1)
	{
		Item = item ?? throw new ArgumentNullException(nameof(item));
		Count = count;
	}

	public override string ToString() => $"[{Item.name} / {Count}]";
}

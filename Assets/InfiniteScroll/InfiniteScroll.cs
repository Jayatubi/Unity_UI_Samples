using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

public class InfiniteScroll : UIBehaviour
{
	[SerializeField]
	private RectTransform itemPrototype;

	[SerializeField, Range(0, 30)]
	int instantateItemCount = 9;

	[SerializeField]
	private Direction direction;

	public OnItemPositionChange onUpdateItem = new OnItemPositionChange();

	[System.NonSerialized]
	public RectTransform[] itemList;
    private int listHead;

    private Vector3[] viewportCorners = new Vector3[4];
    private Vector3[] itemCorners = new Vector3[4];

    protected float diffPreFramePosition = 0;

	protected int currentItemNo = 0;

	public enum Direction
	{
		Vertical,
		Horizontal,
	}

	// cache component

	private RectTransform _rectTransform;
	protected RectTransform rectTransform {
		get {
			if(_rectTransform == null) _rectTransform = GetComponent<RectTransform>();
			return _rectTransform;
		}
	}

	private float anchoredPosition
	{
		get {
			return direction == Direction.Vertical ? -rectTransform.anchoredPosition.y : rectTransform.anchoredPosition.x;
		}
	}

	private float _itemScale = -1;
	public float itemScale {
		get {
			if(itemPrototype != null && _itemScale == -1) {
				_itemScale = direction == Direction.Vertical ? itemPrototype.sizeDelta.y : itemPrototype.sizeDelta.x;
			}
			return _itemScale;
		}
	}

	protected override void Start ()
	{
        var controllers = GetComponents<MonoBehaviour>()
                .Where(item => item is IInfiniteScrollSetup)
                .Select(item => item as IInfiniteScrollSetup)
                .ToList();

        // create items

        var scrollRect = GetComponentInParent<ScrollRect>();
        scrollRect.horizontal = direction == Direction.Horizontal;
        scrollRect.vertical = direction == Direction.Vertical;
        scrollRect.content = rectTransform;

        itemPrototype.gameObject.SetActive(false);

        if (direction == Direction.Horizontal)
        {
            instantateItemCount = (int)((rectTransform.rect.width + itemPrototype.rect.width - 1) / itemPrototype.rect.width) + 1;
        }
        else
        {
            instantateItemCount = (int)((rectTransform.rect.height + itemPrototype.rect.height - 1) / itemPrototype.rect.height) + 1;
        }

        itemList = new RectTransform[instantateItemCount];
        listHead = 0;

        for (int i = 0; i < instantateItemCount; i++)
        {
            var item = GameObject.Instantiate(itemPrototype) as RectTransform;
            item.SetParent(transform, false);
            item.name = i.ToString();
            item.anchoredPosition = direction == Direction.Vertical ? new Vector2(0, -itemScale * i) : new Vector2(itemScale * i, 0);
            itemList[i] = item;

            item.gameObject.SetActive(true);

            foreach (var controller in controllers)
            {
                controller.OnUpdateItem(i, item.gameObject);
            }
        }

        foreach (var controller in controllers)
        {
            controller.OnPostSetupItems();
        }
    }

    void Update()
    {
        if (rectTransform.hasChanged)
        {
            var itemCount = itemList.Length;
            if (itemCount > 0)
            {
                var viewPort = rectTransform.GetComponentInParent<ScrollRect>().viewport;
                if (viewPort != null)
                {
                    viewPort.GetWorldCorners(viewportCorners);

                    bool horizontal = direction == Direction.Horizontal;
                    var offset = (horizontal ? new Vector2(itemScale, 0) : new Vector2(0, -itemScale));
                    while (true)
                    {
                        var item = itemList[listHead];
                        item.GetWorldCorners(itemCorners);
                        // Item's bottom is above the viewport's top
                        if ((!horizontal && itemCorners[0].y > viewportCorners[1].y) ||
                            (horizontal && itemCorners[3].x < viewportCorners[0].x))
                        {
                            var sibling = itemList[(listHead + itemCount - 1) % itemCount];
                            item.anchoredPosition = sibling.anchoredPosition + offset;
                            listHead = (listHead + 1) % itemCount;
                        }
                        else
                        {
                            break;
                        }
                    }

                    while (true)
                    {
                        var listTail = (listHead + itemCount - 1) % itemCount;

                        var item = itemList[listTail];
                        item.GetWorldCorners(itemCorners);
                        // Item's top is below the viewport's bottom
                        if ((!horizontal && itemCorners[1].y < viewportCorners[0].y) ||
                            (horizontal && itemCorners[0].x > viewportCorners[3].x))
                        {
                            var sibling = itemList[listHead];
                            item.anchoredPosition = sibling.anchoredPosition - offset;
                            listHead = listTail;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            rectTransform.hasChanged = false;
        }
    }

    [ContextMenu("Reset")]
    protected override void Reset()
    {
        rectTransform.GetComponentInParent<ScrollRect>().normalizedPosition = Vector2.zero;
    }

    [System.Serializable]
	public class OnItemPositionChange : UnityEngine.Events.UnityEvent<int, GameObject> {}
}

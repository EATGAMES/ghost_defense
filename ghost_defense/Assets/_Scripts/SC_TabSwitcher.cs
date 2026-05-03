using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SC_TabSwitcher : MonoBehaviour
{
    [Serializable]
    private class TabItem
    {
        [Tooltip("탭 버튼 컴포넌트")]
        [SerializeField] private Button button;

        [Tooltip("선택되지 않았을 때 표시할 오브젝트")]
        [SerializeField] private GameObject normalStateObject;

        [Tooltip("선택되었을 때 표시할 오브젝트")]
        [SerializeField] private GameObject selectedStateObject;

        [Tooltip("이 탭을 선택했을 때 보여줄 화면 루트 오브젝트")]
        [SerializeField] private GameObject contentRootObject;

        public Button Button => button;
        public GameObject NormalStateObject => normalStateObject;
        public GameObject SelectedStateObject => selectedStateObject;
        public GameObject ContentRootObject => contentRootObject;
    }

    [Tooltip("탭 순서대로 1번부터 등록할 목록")]
    [SerializeField] private TabItem[] tabs;

    [Tooltip("시작 시 선택할 탭 번호(1부터 시작)")]
    [SerializeField] private int defaultSelectedTabNumber = 3;

    private int currentSelectedIndex = -1;
    private UnityAction[] cachedTabActions;

    private void Awake()
    {
        BindButtonEvents();
        SelectTabByNumber(defaultSelectedTabNumber);
    }

    private void OnDestroy()
    {
        UnbindButtonEvents();
    }

    public void SelectTabByNumber(int tabNumber)
    {
        SelectTab(tabNumber - 1);
    }

    public void SelectTab(int tabIndex)
    {
        if (tabs == null || tabs.Length == 0)
        {
            return;
        }

        if (tabIndex < 0 || tabIndex >= tabs.Length)
        {
            return;
        }

        currentSelectedIndex = tabIndex;
        RefreshVisuals();
    }

    private void BindButtonEvents()
    {
        if (tabs == null)
        {
            return;
        }

        cachedTabActions = new UnityAction[tabs.Length];

        for (int i = 0; i < tabs.Length; i++)
        {
            int capturedIndex = i;

            if (tabs[i] == null || tabs[i].Button == null)
            {
                continue;
            }

            UnityAction action = () => SelectTab(capturedIndex);
            cachedTabActions[i] = action;
            tabs[i].Button.onClick.AddListener(action);
        }
    }

    private void UnbindButtonEvents()
    {
        if (tabs == null)
        {
            return;
        }

        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] == null || tabs[i].Button == null)
            {
                continue;
            }

            if (cachedTabActions != null && i < cachedTabActions.Length && cachedTabActions[i] != null)
            {
                tabs[i].Button.onClick.RemoveListener(cachedTabActions[i]);
            }
        }
    }

    private void RefreshVisuals()
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] == null)
            {
                continue;
            }

            bool isSelected = i == currentSelectedIndex;

            if (tabs[i].NormalStateObject != null)
            {
                tabs[i].NormalStateObject.SetActive(!isSelected);
            }

            if (tabs[i].SelectedStateObject != null)
            {
                tabs[i].SelectedStateObject.SetActive(isSelected);
            }

            if (tabs[i].ContentRootObject != null)
            {
                tabs[i].ContentRootObject.SetActive(isSelected);
            }
        }
    }
}

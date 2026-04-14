const PanelManager = (() => {
    let isPanelManuallyHidden = false;

    const showConditionPanel = () => {
        document.getElementById("conditionInfoPanel").style.display = "block";
        isPanelManuallyHidden = false;
        updateConditionInfoButtonVisibility();
    };

    const hideConditionPanel = () => {
        document.getElementById("conditionInfoPanel").style.display = "none";
        isPanelManuallyHidden = true;
        updateConditionInfoButtonVisibility();
    };

    const updateConditionInfoButtonVisibility = () => {
        const btnOpenConditionInfo = document.getElementById("btnOpenConditionInfo");
        const btnCollapsibleConditions = document.querySelector(".btn-collapsible-conditions");
        const conditionPanel = document.getElementById("conditionInfoPanel");
        const state = ConditionManager.getState();
        const hasVisibleConditions = Object.keys(state.visibleConditions).length > 0;
        const isPanelHidden = conditionPanel.style.display === "none";
        
        if (hasVisibleConditions && isPanelHidden) {
            btnOpenConditionInfo.style.display = "block";
            btnCollapsibleConditions.classList.remove("btn-full-width");
        } else {
            btnOpenConditionInfo.style.display = "none";
            btnCollapsibleConditions.classList.add("btn-full-width");
        }
    };

    const showPanelIfNotHidden = () => {
        const state = ConditionManager.getState();
        if (!isPanelManuallyHidden && Object.keys(state.visibleConditions).length > 0) {
            showConditionPanel();
        }
    };

    const initializeEventListeners = () => {
        document.getElementById("closeConditionInfo").addEventListener("click", () => {
            hideConditionPanel();
            updateConditionInfoButtonVisibility();
        });

        document.getElementById("btnOpenConditionInfo").addEventListener("click", () => {
            ConditionManager.reloadActiveConditions();
            showConditionPanel();
            updateConditionInfoButtonVisibility();
        });
    };

    return {
        showConditionPanel,
        hideConditionPanel,
        updateConditionInfoButtonVisibility,
        showPanelIfNotHidden,
        initializeEventListeners
    };
})();

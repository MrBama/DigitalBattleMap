const PanelManager = (() => {
    const setButtonActiveState = (isActive) => {
        const btn = document.getElementById("btnOpenConditionInfo");
        if (isActive) {
            btn.classList.add("active");
        } else {
            btn.classList.remove("active");
        }
    };

    const showConditionPanel = () => {
        document.getElementById("conditionInfoPanel").style.display = "block";
        setButtonActiveState(true);
    };

    const hideConditionPanel = () => {
        document.getElementById("conditionInfoPanel").style.display = "none";
        setButtonActiveState(false);
    };

    const initializeEventListeners = () => {
        document.getElementById("closeConditionInfo").addEventListener("click", () => {
            hideConditionPanel();
        });

        document.getElementById("btnOpenConditionInfo").addEventListener("click", () => {
            const conditionPanel = document.getElementById("conditionInfoPanel");
            if (conditionPanel.style.display === "none" || conditionPanel.style.display === "") {
                ConditionManager.reloadActiveConditions();
                showConditionPanel();
            } else {
                hideConditionPanel();
            }
        });
    };

    return {
        showConditionPanel,
        hideConditionPanel,
        initializeEventListeners
    };
})();

const ConditionManager = (() => {
    const state = {
        activeConditions: {},
        visibleConditions: {},
        closedConditions: {}
    };

    const addConditionInfo = (conditionName, callback) => {
        $.ajax({
            url: "Navigation/GetConditionInfo",
            type: "GET",
            data: { 'conditionName': conditionName },
            success: (response) => {
                const conditionData = JSON.parse(response);
                const version = SettingsManager.getConditionVersion();
                
                if (conditionData.versions && conditionData.versions[version]) {
                    const versionData = conditionData.versions[version];
                    state.activeConditions[conditionName] = versionData;
                    ConditionRenderer.renderConditionCard(conditionName, versionData);
                    
                    if (callback) callback();
                }
            },
            error: (error) => {
                console.error("Error loading condition info:", error);
                if (callback) callback();
            }
        });
    };

    const removeConditionInfo = (conditionName) => {
        delete state.visibleConditions[conditionName];
        state.closedConditions[conditionName] = true;
        ConditionRenderer.renderConditionCards();
        PanelManager.updateConditionInfoButtonVisibility();
    };

    const syncWithAppliedConditions = (appliedButtonIds) => {
        const appliedConditionNames = appliedButtonIds.map(buttonId => {
            const button = document.getElementById(buttonId);
            return button ? button.querySelector('.span-condition').textContent : null;
        }).filter(name => name !== null);

        Object.keys(state.visibleConditions).forEach(conditionName => {
            if (!appliedConditionNames.includes(conditionName)) {
                delete state.visibleConditions[conditionName];
            }
        });

        Object.keys(state.closedConditions).forEach(conditionName => {
            if (!appliedConditionNames.includes(conditionName)) {
                delete state.closedConditions[conditionName];
            }
        });

        const conditionsToLoad = [];
        appliedConditionNames.forEach(conditionName => {
            if (!state.closedConditions[conditionName] && !state.visibleConditions[conditionName]) {
                state.visibleConditions[conditionName] = true;
                if (!state.activeConditions[conditionName]) {
                    conditionsToLoad.push(conditionName);
                }
            }
        });

        if (conditionsToLoad.length === 0) {
            ConditionRenderer.renderConditionCards();
            PanelManager.showPanelIfNotHidden();
            return;
        }

        let loadedCount = 0;
        conditionsToLoad.forEach(conditionName => {
            addConditionInfo(conditionName, () => {
                loadedCount++;
                if (loadedCount === conditionsToLoad.length) {
                    ConditionRenderer.renderConditionCards();
                    PanelManager.showPanelIfNotHidden();
                }
            });
        });
    };

    const reloadActiveConditions = () => {
        const conditionsToLoad = Object.keys(state.visibleConditions).filter(name => !state.activeConditions[name]);
        
        if (conditionsToLoad.length === 0) {
            ConditionRenderer.renderConditionCards();
            return;
        }
        
        let loadedCount = 0;
        conditionsToLoad.forEach(conditionName => {
            addConditionInfo(conditionName, () => {
                loadedCount++;
                if (loadedCount === conditionsToLoad.length) {
                    ConditionRenderer.renderConditionCards();
                }
            });
        });
    };

    return {
        addConditionInfo,
        removeConditionInfo,
        syncWithAppliedConditions,
        reloadActiveConditions,
        getState: () => state
    };
})();

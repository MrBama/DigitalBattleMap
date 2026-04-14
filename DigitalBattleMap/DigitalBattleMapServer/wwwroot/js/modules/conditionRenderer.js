const ConditionRenderer = (() => {
    const renderConditionCard = (conditionName, versionData) => {
        const container = document.getElementById("conditionCardsContainer");
        
        if (!document.getElementById(`condition-card-${conditionName}`)) {
            const card = document.createElement("div");
            card.className = "condition-card";
            card.id = `condition-card-${conditionName}`;
            const titleContent = versionData.link && versionData.link !== ""
                ? `<div class="condition-card-title-wrapper"><a class="condition-card-title-link" href="${versionData.link}" target="_blank">${conditionName}</a></div>`
                : `<h6 class="condition-card-title">${conditionName}</h6>`;
            
            card.innerHTML = `
                <div class="condition-card-header">
                    <img class="condition-card-icon" src="/ConditionIcons/${conditionName}.png" alt="${conditionName}" />
                    ${titleContent}
                    <button type="button" class="condition-card-close" data-condition="${conditionName}">
                        <span aria-hidden="true">&times;</span>
                    </button>
                </div>
                <p class="condition-card-description">${versionData.description}</p>
            `;
            container.appendChild(card);
            
            card.querySelector(".condition-card-close").addEventListener("click", (e) => {
                e.stopPropagation();
                ConditionManager.removeConditionInfo(conditionName);
            });
        }
    };

    const renderConditionCards = () => {
        const container = document.getElementById("conditionCardsContainer");
        container.innerHTML = "";
        
        const state = ConditionManager.getState();
        Object.entries(state.activeConditions).forEach(([conditionName, versionData]) => {
            if (state.visibleConditions[conditionName]) {
                renderConditionCard(conditionName, versionData);
            }
        });
    };

    return {
        renderConditionCard,
        renderConditionCards
    };
})();

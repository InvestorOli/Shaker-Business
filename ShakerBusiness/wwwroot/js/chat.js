window.oliChatScrollToBottom = (elementId) => {
    const el = document.getElementById(elementId);
    if (el) {
        el.scrollTop = el.scrollHeight;
    }
};

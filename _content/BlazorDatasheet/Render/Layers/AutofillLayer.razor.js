function findScrollableAncestor(element) {
    if (!element)
        return null;

    let parent = element.parentElement;

    if (parent == null || element === document.body || element === document.documentElement)
        return null;

    const style = window.getComputedStyle(parent);
    if (style.overflowY !== 'visible' || style.overflowX !== 'visible' || style.overflow !== 'visible')
        return parent;

    return findScrollableAncestor(parent);
}

export function getScrollableOffset(element) {
    const ancestor = findScrollableAncestor(element) ?? document.documentElement;

    return {
        x: ancestor.scrollLeft,
        y: ancestor.scrollTop
    };
}

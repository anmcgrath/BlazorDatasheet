import { findScrollableAncestor } from "/_content/BlazorDatasheet/js/scroll-utils.js";

export function getScrollableOffset(element) {
    const ancestor = findScrollableAncestor(element) ?? document.documentElement;

    return {
        x: ancestor.scrollLeft,
        y: ancestor.scrollTop
    };
}

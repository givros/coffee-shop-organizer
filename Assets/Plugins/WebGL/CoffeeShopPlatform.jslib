mergeInto(LibraryManager.library, {
  CoffeeShop_IsMobileOrTouch: function () {
    var nav = typeof navigator !== "undefined" ? navigator : {};
    var mobileUserAgent = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(nav.userAgent || "");
    var mobileHint = nav.userAgentData && nav.userAgentData.mobile === true;
    var touchPoints = nav.maxTouchPoints || nav.msMaxTouchPoints || 0;
    var coarsePointer = typeof window !== "undefined" &&
      window.matchMedia &&
      window.matchMedia("(pointer: coarse)").matches;
    return mobileUserAgent || mobileHint || touchPoints > 0 || coarsePointer ? 1 : 0;
  }
});

mergeInto(LibraryManager.library, {
  $MachPointer: {
    initialized: false, x: 0, y: 0, readyAt: 0,
    reset: function () {
      MachPointer.x = MachPointer.y = 0;
      MachPointer.readyAt = performance.now() + 80;
    },
    init: function () {
      if (MachPointer.initialized) return;
      MachPointer.initialized = true;
      document.addEventListener('pointerlockchange', MachPointer.reset);
      document.addEventListener('visibilitychange', MachPointer.reset);
      window.addEventListener('blur', MachPointer.reset);
      window.addEventListener('focus', MachPointer.reset);
      document.addEventListener('mousemove', function (event) {
        if (document.pointerLockElement !== Module.canvas || document.hidden || performance.now() < MachPointer.readyAt) return;
        // Relative browser motion avoids Unity's screen-position recentering deltas.
        var x = event.movementX, y = event.movementY;
        if (!Number.isFinite(x) || !Number.isFinite(y)) return;
        // A single event spanning a large part of the screen is a warp, not a turn.
        if (Math.abs(x) > 256 || Math.abs(y) > 256) return;
        MachPointer.x += x;
        MachPointer.y -= y; // DOM down-positive -> Unity up-positive.
      }, true);
      MachPointer.reset();
    }
  },
  Mach_ReadPointerDelta__deps: ['$MachPointer'],
  Mach_ReadPointerDelta: function (output) {
    MachPointer.init();
    var active = document.pointerLockElement === Module.canvas && !document.hidden && document.hasFocus();
    var x = MachPointer.x, y = MachPointer.y;
    MachPointer.x = MachPointer.y = 0;
    // Drop an accumulated backlog after a long browser/render stall.
    if (!active || performance.now() < MachPointer.readyAt || Math.abs(x) > 600 || Math.abs(y) > 600) x = y = 0;
    HEAPF32[output >> 2] = x;
    HEAPF32[(output >> 2) + 1] = y;
    return active ? 1 : 0;
  }
});

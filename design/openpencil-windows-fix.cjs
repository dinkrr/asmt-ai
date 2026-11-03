const descriptor = Object.getOwnPropertyDescriptor(URL.prototype, 'pathname');

Object.defineProperty(URL.prototype, 'pathname', {
  ...descriptor,
  get() {
    const pathname = descriptor.get.call(this);
    if (process.platform === 'win32' && /\/canvaskit-wasm\/bin\/(?:full\/)?(?:canvaskit\.wasm)?$/.test(pathname) && /^\/[A-Za-z]:\//.test(pathname)) {
      return pathname.slice(1);
    }
    return pathname;
  }
});
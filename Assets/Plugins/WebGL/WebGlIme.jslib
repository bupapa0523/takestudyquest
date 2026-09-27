mergeInto(LibraryManager.library, {
  WebGlIme_Open: function (textPtr, x, y, w, h) {
    var initial = UTF8ToString(textPtr);
    var input = document.getElementById("unity-ime-input");
    if (!input) {
      input = document.createElement("input");
      input.id = "unity-ime-input";
      input.type = "text";
      input.lang = "ja";
      input.setAttribute("lang", "ja");
      input.setAttribute("autocomplete", "off");
      input.setAttribute("placeholder", "ここに日本語で入力");
      input.style.position = "fixed";
      input.style.zIndex = "100000";
      input.style.boxSizing = "border-box";
      input.style.fontSize = "22px";
      input.style.fontWeight = "700";
      input.style.padding = "6px 12px";
      input.style.border = "3px solid #c48a12";
      input.style.borderRadius = "8px";
      input.style.background = "#fff6df";
      input.style.color = "#1a1208";
      input.style.caretColor = "#1a1208";
      input.style.outline = "none";
      input.style.boxShadow = "0 6px 18px rgba(0,0,0,0.45)";
      document.body.appendChild(input);
      var send = function () {
        if (typeof SendMessage === "function")
          SendMessage("WebGlIme", "OnImeText", input.value);
      };
      input.addEventListener("input", send);
      input.addEventListener("compositionupdate", send);
      input.addEventListener("compositionend", send);
      input.addEventListener("keydown", function (e) {
        e.stopPropagation();
        if (e.key === "Enter") {
          e.preventDefault();
          send();
          if (typeof SendMessage === "function")
            SendMessage("WebGlIme", "OnImeSubmit", input.value);
        } else if (e.key === "Escape") {
          if (typeof SendMessage === "function")
            SendMessage("WebGlIme", "OnImeCancel", "");
        }
      });
      input.addEventListener("blur", function () {
        send();
        input.style.display = "none";
        if (typeof SendMessage === "function")
          SendMessage("WebGlIme", "OnImeBlur", input.value);
      });
    }
    var canvas = document.querySelector("#unity-canvas");
    var rect = canvas ? canvas.getBoundingClientRect() : { left: 0, top: 0, width: window.innerWidth, height: window.innerHeight };
    var bw = canvas && canvas.width ? canvas.width : rect.width;
    var bh = canvas && canvas.height ? canvas.height : rect.height;
    var sx = bw > 0 ? rect.width / bw : 1;
    var sy = bh > 0 ? rect.height / bh : 1;
    input.style.left = (rect.left + x * sx) + "px";
    input.style.top = (rect.top + y * sy) + "px";
    input.style.width = Math.max(80, w * sx) + "px";
    input.style.height = Math.max(32, h * sy) + "px";
    input.style.transform = "none";
    input.style.display = "block";
    input.value = initial || "";
    setTimeout(function () { input.focus(); }, 40);
  },
  WebGlPhoto_Open: function () {
    var input = document.getElementById("unity-photo-input");
    if (!input) {
      input = document.createElement("input");
      input.id = "unity-photo-input";
      input.type = "file";
      input.accept = "image/png,image/jpeg,image/jpg,.png,.jpg,.jpeg";
      input.style.display = "none";
      input.addEventListener("change", function () {
        var file = input.files && input.files[0];
        input.value = "";
        if (!file) return;
        var reader = new FileReader();
        reader.onload = function () {
          var img = new Image();
          img.onload = function () {
            var canvas = document.createElement("canvas");
            canvas.width = 256;
            canvas.height = 256;
            var ctx = canvas.getContext("2d");
            var side = Math.min(img.width, img.height);
            var sx = (img.width - side) / 2;
            var sy = (img.height - side) / 2;
            ctx.drawImage(img, sx, sy, side, side, 0, 0, 256, 256);
            var url = canvas.toDataURL("image/png");
            var comma = url.indexOf(",");
            var b64 = comma >= 0 ? url.substring(comma + 1) : url;
            if (typeof SendMessage === "function")
              SendMessage("WebGlIme", "OnPhotoPicked", b64);
          };
          img.src = reader.result;
        };
        reader.readAsDataURL(file);
      });
      document.body.appendChild(input);
    }
    input.click();
  },
  WebGlIme_Close: function () {
    var input = document.getElementById("unity-ime-input");
    if (!input) return;
    input.style.display = "none";
    input.blur();
  }
});

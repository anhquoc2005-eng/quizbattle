# Third-party notices

QuizBattle dùng các thành phần của bên thứ ba dưới đây để vẽ nhân vật (avatar) ở frontend. Phiên bản được ghim chính xác trong `frontend/package.json`.

| Gói | Phiên bản | Giấy phép | Nguồn |
|---|---|---|---|
| `@dicebear/core` | 10.6.0 | MIT | https://github.com/dicebear/dicebear |
| `@dicebear/styles` (chỉ file `notionists.json`) | 10.6.0 | CC0 1.0 (hình vẽ); MIT (mã và manifest của gói) | https://github.com/dicebear/styles |

Chỉ file `notionists.json` của `@dicebear/styles` được nạp vào bản build. Các kiểu khác trong cùng gói (một số dùng giấy phép CC BY 4.0) không được import nên không nằm trong bản build của QuizBattle.

Ứng dụng vẽ SVG hoàn toàn ở trình duyệt từ các gói trên; không gọi API HTTP công khai `api.dicebear.com`.

## Hình vẽ "Notionists" (CC0 1.0)

Kiểu nhân vật dùng bộ hình **Notionists**, do **Zoish** thiết kế:

- Bản gốc: https://heyzoish.gumroad.com/l/notionists (tác giả: https://bio.link/heyzoish)
- Bản phối lại (remix) đóng gói trong `@dicebear/styles`, giấy phép **CC0 1.0 Universal (Public Domain Dedication)**: https://creativecommons.org/publicdomain/zero/1.0/
- Metadata trong file `notionists.json` ghi: license "CC0 1.0", creator "Zoish", source "Notionists".

CC0 không đòi hỏi ghi công; thông tin trên được nêu để minh bạch nguồn gốc.

## `@dicebear/core` và mã của `@dicebear/styles` (MIT)

```text
MIT License

Copyright (c) 2026 Florian Körner

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

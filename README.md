# TSviewACD
TS viewer for Amazon Cloud Drive

## Overview
Amazon Driveに保存した動画や音楽ファイルをストリーミング再生するソフト

## Description
TSファイルをUDP送信する機能に加え、FFmpegのデコーダを使って
ストリーミング再生する機能があります。
基本的なファイル操作もでき、アップロード、ダウンロード、フォルダの作成、ファイルの移動、削除等が可能です。
AES-256 CTRモードと、[CarotDAV](http://www.rei.to/carotdav.html "CarotDAV")
互換の暗号化ができ、暗号化したファイルも透過的にストリーミング再生ができます。

## Requirement
このプログラムは、c# .NET 4.5.2でコンパイルされています。
Microsoft .NET Framework 4.5.2のランタイムが必要となる場合があります。
<https://www.microsoft.com/ja-JP/download/details.aspx?id=42643>

このプログラムは、Visual Studio 2015 C++でコンパイルされています。
Visual Studio 2015 の Visual C++ 再頒布可能パッケージが必要になります。
<https://www.microsoft.com/ja-jp/download/details.aspx?id=53587>

## License
[NYSL 煮るなり焼くなり好きにしろライセンス](http://www.kmonos.net/nysl/index.ja.html "NYSL")

## Install
バイナリはサイトで配布しております<https://lithium03.info/product/TSvACD.html>
* インストーラ版　<https://lithium03.info/product/TSviewACD/TSviewACD_20170218_installer.zip>
* ポータブル版 <https://lithium03.info/product/TSviewACD/TSviewACD_20170218.zip>

## Contribution
1. Fork it ( http://github.com/lithium0003/TSviewACD/fork )
2. Create your feature branch (git checkout -b my-new-feature)
3. Commit your changes (git commit -am 'Add some feature')
4. Push to the branch (git push origin my-new-feature)
5. Create new Pull Request

コンパイルをする際には、ルートにexternalフォルダを以下の内容で生成する必要があります。
コンパイル済みのものをサイトで配布しております。
<https://lithium03.info/product/TSviewACD/external-20170218.zip>
- external
    - SDL2 ([SDL2][]及び[SDL_tiff][])
        + include
        - lib
        - lib32
    - ffmpeg ([FFmpeg][])
        + include
        - lib
        - lib32
    - bin (64bitインストーラ版用dllフォルダ)
    - bin32 (32bitインストーラ版用dllフォルダ)
    + _installed.txt

[SDL2]: https://www.libsdl.org/download-2.0.php "SDL2"
[SDL_tiff]: https://www.libsdl.org/projects/SDL_ttf/ "SDL_tiff"
[FFmpeg]: https://ffmpeg.org/ "FFmpeg"

必要な外部ライブラリのコンパイル方法については、<https://lithium03.info/product/TSviewACD/FFmpeg-SDL2.html>を参照。

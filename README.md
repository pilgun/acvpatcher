
# ACVPatcher

[![Build](https://github.com/pilgun/acvpatcher/actions/workflows/build.yml/badge.svg?branch=main&event=push)](https://github.com/pilgun/acvpatcher/actions/workflows/build.yml)

ACVPatcher is a CLI tool to patch AndroidManifest and DEX classes inside an APK without reassembling resources. ACVPatcher is a replacement of Apktool for the ACVTool purpuses to improve its repackaging success rate.

## Installation

- Checkout [Releases](https://github.com/pilgun/acvpatcher/releases) for the last prebuilt binary
- `chmod +x ACVPatcher` to make it executable

## Usage

ACVPatcher updates DEX classes and/or AndroidManifest inside the APK file. ACVPatcher may insert new permissions, a broadcast receiver, and instrumentation tag into AndroidManifest through corresponding options at once. The APK is signed and zip aligned.

- Adding permissions to AndroidManifest

    ```shell
    $ acvpatcher -a ./base.apk --permission android.permission.WRITE_EXTERNAL_STORAGE android.permission.READ_EXTERNAL_STORAGE
    ```

- Adding a receiver to AndroidManifest

    This example will add the AcvReceiver receiver tag with two intent filters (`calculate` and `snap`)

    ```shell
    $ acvpatcher -a ./base.apk --receiver tool.acv.AcvReceiver:tool.acv.calculate tool.acv.AcvReceiver:tool.acv.snap
    ```

- Rewritting DEX files

    ```shell
    $ acvpatcher -a ./base.apk --class ./classes.dex ./classes2.dex
    ```

- Simply re-sign the app
    ```shell
    $ acvpatcher -a ./base.apk
    ```

## AndroidManifest Patching

Here is an example of XML added to patched AndroidManifest

```xml
<manifest ...>
    ...
    <application>
        ...
        <receiver
            android:name="tool.acv.AcvReceiver"
            android:enabled="true"
            android:exported="true">
            <intent-filter>
                <action android:name="tool.acv.snap" />
                <action android:name="tool.acv.calculate" />
                <action android:name="tool.acv.flush" />
            </intent-filter>
        </receiver>
    </application>
    <instrumentation
        android:name="tool.acv.AcvInstrumentation"
        android:targetPackage="package.name" />
    <uses-permission
        android:name="android.permission.WRITE_EXTERNAL_STORAGE" />
</manifest>
```


# Acknowledgement

ACVPatcher is built on top of QuestPatcher modules initially developed by @Lauriethefish.

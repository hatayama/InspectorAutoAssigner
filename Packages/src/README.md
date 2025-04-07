# Inspector Link Binder

Inspector Link Binderは、UnityのInspectorで値を他のコンポーネントにバインドするためのパッケージなのだ！

## 特徴

- Inspectorの値を他のコンポーネントに簡単にバインド
- リアルタイムでの値の同期
- カスタマイズ可能なバインディングルール

## インストール方法

### OpenUPM経由

```bash
openupm add com.your-org.inspector-link-binder
```

### Unity Package Manager経由

1. Window > Package Managerを開く
2. 「+」ボタンをクリック
3. 「Add package from git URL」を選択
4. 以下のURLを入力：
   ```
   https://github.com/your-org/inspector-link-binder.git
   ```

## 使い方

1. バインドしたいコンポーネントに`InspectorLinkBinder`コンポーネントを追加
2. ソースとなるInspectorの値を設定
3. ターゲットとなるコンポーネントとプロパティを設定

## ライセンス

MIT License

## 作者

Your Name 
# AutoAssignment ObjectField

AutoAssignment ObjectFieldは、UnityのInspectorで値を自動で設定するツールです。

## 使い方

1. このパッケージをimportすると、InspectorのObjectFieldに虫眼鏡アイコンが表示されます。
2. 虫眼鏡アイコンをクリックすると、自分自身と子要素を検索し、変数の型にあったコンポーネントを自動で割り当てます。
3. 変数の型を同じコンポーネントが複数見つかった場合、変数名と一致するコンポーネントを優先的に割り当てます。
4. 変数名と一致するコンポーネントが複数ある場合は、popupが表示され、選択することができます。

## インストール方法

### OpenUPM経由

```bash
openupm add io.github.hatayama.inspectorautoassigner
```

### Unity Package Manager経由

1. Window > Package Managerを開く
2. 「+」ボタンをクリック
3. 「Add package from git URL」を選択
4. 以下のURLを入力：
   ```
   https://github.com/hatayama/InspectorAutoAssigner.git
   ```

## ライセンス

MIT License

## 作者

Masamichi Hatayama
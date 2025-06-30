# InspectorAutoAssigner
[![license](https://img.shields.io/badge/LICENSE-MIT-green.svg)](LICENSE.md)
![](https://img.shields.io/badge/Unity-2020.3+-57b9d3.svg?style=flat&logo=unity)
[![](https://img.shields.io/twitter/follow/ssr_maguro.svg?label=Follow&style=social)](https://x.com/ssr_maguro)

InspectorAutoAssigner is a tool that automatically assigns values in the Unity Inspector.

## Usage

1. Once you have imported this package, you will see a magnifying glass icon in the ObjectField of the Unity Inspector.
2. Click the magnifying glass icon to search for both yourself and child elements, and automatically assign components that match the variable type.
![1](https://github.com/hatayama/InspectorAutoAssigner/blob/main/Assets/Images/1.png?raw=true)
3. If multiple components with the same name are found, a popup will appear and you can press the candidate name to ping it, or the button next to it to assign it.
![3](https://github.com/hatayama/InspectorAutoAssigner/blob/main/Assets/Images/2.png?raw=true)
4. If multiple components of the same type are found, prioritize components that match the variable name.
![2](https://github.com/hatayama/InspectorAutoAssigner/blob/main/Assets/Images/3.png?raw=true)



## Installation
### Unity Package Manager via

1. Open Window > Package Manager
2. Click the "+" button
3. Select "Add package from git URL"
4. Enter the following URL:
   ```
   https://github.com/hatayama/InspectorAutoAssigner.git?path=/Packages/src
   ```

### OpenUPM via

### How to use UPM with Scoped registry in Unity Package Manager
1. Open the Project Settings window and navigate to the Package Manager page.
2. Add the following entry to the Scoped Registries list:
```
Name：OpenUPM
URL: https://package.openupm.com
Scope(s)：io.github.hatayama.inspectorautoassigner
```
![](https://github.com/hatayama/_Docs/blob/main/Images/upm_pckage1.png?raw=true)

3. Open the Package Manager window, navigate to the "hatayama" page in the My Registries section.
![](https://github.com/hatayama/_Docs/blob/main/Images/upm_pckage2.png?raw=true)


### Command
```bash
openupm add io.github.hatayama.inspectorautoassigner
```


## License

MIT License

## Author

Masamichi Hatayama

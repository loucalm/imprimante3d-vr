# Imprimante 3D VR — TP 3.1 / 3.2 (WR507D Game Dev VR)

Simulation VR de la mise en route d'une imprimante 3D : interrupteur à l'arrière, portes à charnière, clé USB à brancher
dans un port (Socket Interactor), écran tactile, checklist et flèches de guidage.

- Unity **6000.3.23f1**, URP, XR Interaction Toolkit 3.3.2, OpenXR (Meta Quest)
- Scène VR : `Assets/Scenes/BasicScene.unity` (manettes avec mains modélisées « Oculus Hands »)
- Scène web : `Assets/Scenes/BasicScene_Web.unity` (souris / clavier, pour un build WebGL)

## Étapes du guide
1. Allumer l'imprimante (interrupteur à l'arrière) 2. Inspecter le compartiment (toucher une porte)
3. Brancher la clé USB 4. Appuyer sur « Imprimer ».

## Scripts
`Assets/Scripts/Imprimante3D` (`SwitchBehaviour`, `ImprimanteBehaviour`, `ArrowBob`), `Assets/Scripts/Checklist/StepManager.cs`,
`Assets/Scripts/HandAnimator.cs`, `Assets/Scripts/Web/*` (interactions souris pour le WebGL).

## Menus d'éditeur (`Assets/Editor`)
- `TP > Build Imprimante Scene` puis `TP > Build TP3.2 - Checklist` : reconstruisent la scène VR.
- `TP > Build Web Scene` puis `TP > Build WebGL` : scène web et build (dossier `../WebGL_Build`).

## Web
Glisser : regarder · ZQSD / flèches : se déplacer · Molette : zoom · Clic : interagir.
Pour l'héberger : déposer le dossier du build tel quel ; activer gzip pour `.wasm`, `.js`, `.data` et servir `.wasm` en `application/wasm`.

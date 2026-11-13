@echo off
rem Run once from the project root, before the first commit of art.
git lfs install
git lfs track "*.fbx" "*.obj" "*.blend" "*.jpg" "*.jpeg" "*.png" "*.tga" "*.psd" "*.wav" "*.mp3" "*.ogg" "*.mp4" "*.mov"
git add .gitattributes
echo Git LFS rules are in .gitattributes. Art already committed without LFS stays as it is: ask Person 1 before rewriting history.

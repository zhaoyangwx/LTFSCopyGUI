setlocal ENABLEDELAYEDEXPANSION
for %%f in (LTFSIndex_Autosave_*.schema) do (
set fname=%%f
echo !fname!
echo !fname:~19,8!
ren !fname! !fname:~19,8!.schema
)
for %%f in (LTFSIndex_Autosave_*.cm) do (
set fname=%%f
echo !fname!
echo !fname:~19,8!
ren !fname! !fname:~19,8!.cm
)
@echo off
setlocal EnableDelayedExpansion

rem MSBuild generated code does not compile
rem Pubmed  NullReferenceException during code generation
rem Rss     ArgumentNullException  during code generation
rem XMLSpec ArgumentException      during code generation
rem XSD     ArgumentNullException  during code generation
for /D %%D in (*) do (
  set folder=%%~nxD
  set skip=false
  for %%x in (obj bin) do (
    if /I !folder! == %%x set skip=true
  )
  for %%x in (MSBuild) do (
    if /I !folder! == %%x (
      set skip=true
      echo [33mSkipping !folder! ^(generated code does not compile^)[0m 
    )
  )
  for %%x in (Pubmed Rss XMLSpec XSD) do (
    if /I !folder! == %%x (
      set skip=true
      echo [31mSkipping !folder! ^(exception during code generation^)[0m 
    )
  )
  if !skip! == false (
    echo [0;1mGenerating code in !folder![0m 
    pushd !folder!

    if /I !folder! == XMLSpec (
      linqtoxsd gen . -c . xmlspec.xsd
    ) else (
      linqtoxsd gen . -c . >nul
    )

    popd
  )
)

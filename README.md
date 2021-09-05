# JassToTs
Simple Jass to typescript translator. Main project writed in C# for Net5.0 runtime.

## Build project
For building you need Net5.0 or above. In Root directory use next command:
```shell
dotnet publish -c Release
```

## Usage
#### Help message:
```powershell
.\JassToTs.exe -h
```
#### For translate example.j:
```powershell
.\JassToTs.exe -i example.j
```
it will be created example.ts file
#### For translate example.j into sample.ts:
```powershell
.\JassToTs.exe -i example.j -o sample.ts
```
for diffrent output file.
#### interactive mode
In this mode you can type all flags. One line one command line argument. In the end pass empty line
```powershell
.\JassToTs.exe -input
```
```
please enter arguments, empty line for continue
-i
example.j


JASS to TypeScript translator (by VADIMREX)

reading file example.j
lexeing
parsing
translating
saving into example.ts
```

## Plans
 - [x] Lexer
 - [x] Parser
 - [x] To typescript translator
 - [x] To lua translator
 - [x] To galaxy translator
 - [ ] Collect Parse errors and proceed to end of file
 - [ ] Code Optimization
   - [x] omit empty else block
   - [x] in if/then/else omit bracers when block contains one statement
   - [x] omit extra brackets in nesting brackets
   - [ ] if possible translate loops into while(condition) or for(...)
 - [ ] YDWE compatibility
   - [ ] *(Possibly)* macro preprocessing
 - [ ] *(Possibly)* interpreter
 - [ ] Java version
   - [x] Lexer
   - [ ] Parser
   - [ ] To typescript translator
   - [ ] To lua translator
 - [ ] Go version
 - [ ] *(Possibly)* Python version


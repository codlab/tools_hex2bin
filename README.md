tools_hex2bin
=============

Converter to generate BIN files out of HEX files and related MD5 sums.

# Windows

A GUI version is provded in this directory

# Linux/MacOS CLI

A CLI version is available in the cli/ folder to enhance the automation of update release

## Compilation

- install mono into your environment (to have mono and mcs)
- compile the Hex2Bin.cs to Hex2bin.exe
- prepare the environment to have your binary version available

## Shell execution

```
cd ./cli
mcs Hex2Bin.cs
mkdir -p ~/bin/mono
mv Hex2Bin.exe ~/bin/mono
cp ./cli/hex2bin ~/bin
chmod +x ./cli/hex2bin
```

## PATH modification

make sure you have in your .bashrc, .bash_profile, etc...
```
export PATH=$PATH:~/bin
```

## Usage

```
hex2bin ~/path/to/the/hex/file.hex
```

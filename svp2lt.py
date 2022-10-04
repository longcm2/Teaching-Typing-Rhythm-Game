import sys
import re
import argparse
from pathlib import Path

parser = argparse.ArgumentParser()

parser.add_argument("-i", "--input", dest = "input", default = None, help = "Path to input")
args = parser.parse_args()

file_path = Path(args.input)
rawinput = [line.rstrip('\n') for line in open(file_path)]


for word in rawinput:
    re.sub("(.)*\{\"onset", "\{\"onset", word)
    re.sub("\, \"detune(.)*", "", word)
    print(word)

#exit()
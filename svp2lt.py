import sys
import re
import argparse
from pathlib import Path

parser = argparse.ArgumentParser()

parser.add_argument("-i", "--input", dest = "input", default = None, help = "Path to input")
parser.add_argument("-t", "--title", dest = "title", default = None, help = "Title of the song")
parser.add_argument("-a", "--artist", dest = "artist", default = None, help = "Artist of the song")
parser.add_argument("-s", "--scale", dest = "scale", default = None, help = "Scales the note timing -- 0.5 in most cases")
parser.add_argument("-o", "--offset", dest = "offset", default = None, help = "How many seconds the notes should be offset to match the audio -- different per song, usually I reccomend 0.1 seconds before perfect")
args = parser.parse_args()

file_path = Path(args.input)
title = Path(args.title)
artist = Path(args.artist)
scale = Path(args.scale)
offset = Path(args.offset)
rawinput = [line.rstrip('\n') for line in open(file_path)]

print(title, "\n", artist, "\n", scale, "\n", offset, "\n")

for word in rawinput:
    re.sub("(.)*\\{\"onset", "\\{\"onset", word)
    re.sub("\\, \"detune(.)*", "", word)
    print(word)
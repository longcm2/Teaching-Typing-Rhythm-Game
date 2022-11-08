import sys
import re
import argparse
from pathlib import Path

parser = argparse.ArgumentParser()

parser.add_argument("-i", "--input", dest = "input", default = None, help = "Path to input")
parser.add_argument("-t", "--title", dest = "title", default = None, help = "Title of the song")
parser.add_argument("-a", "--artist", dest = "artist", default = None, help = "Artist of the song")
parser.add_argument("-s", "--tempo", dest = "tempo", default = None, help = "The tempo of the song in BPM")
parser.add_argument("-o", "--offset", dest = "offset", default = None, help = "How many seconds the notes should be offset to match the audio -- different per song, usually I reccomend 0.1 seconds before perfect")
args = parser.parse_args()

file_path = Path(args.input)
title = Path(args.title)
artist = Path(args.artist)
tempo = Path(args.tempo)
offset = Path(args.offset)

contents = open(file_path).read()
re.sub("onset", "\nonset", contents)

print(contents)

#print(title,"\n",artist,"\n",tempo,"\n",offset,"\n")

#for word in rawinput:
#    temp = word
#    re.sub("(.)*onset", "onset", temp)
#    re.sub("detune(.)*", "detune", temp)
#    print(temp)
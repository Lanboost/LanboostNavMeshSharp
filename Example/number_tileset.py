import scipy.misc
import numpy as np
import imageio

representations = {
    '0': ('###', '# #', '# #', '# #', '###'),
    '1': ('  #', '  #', '  #', '  #', '  #'),
    '2': ('###', '  #', '###', '#  ', '###'),
    '3': ('###', '  #', '###', '  #', '###'),
    '4': ('# #', '# #', '###', '  #', '  #'),
    '5': ('###', '#  ', '###', '  #', '###'),
    '6': ('###', '#  ', '###', '# #', '###'),
    '7': ('###', '  #', '  #', '  #', '  #'),
    '8': ('###', '# #', '###', '# #', '###'),
    '9': ('###', '# #', '###', '  #', '###'),
    '.': ('   ', '   ', '   ', '   ', '  #'),
}


tile_size = 32
num_size = 2
# Image size
width = tile_size*10
height = tile_size*10
channels = 4

# Create an empty image
img = np.zeros((height, width, channels), dtype=np.uint8)

# Draw something (http://stackoverflow.com/a/10032271/562769)
xx, yy = np.mgrid[:height, :width]
circle = (xx - 100) ** 2 + (yy - 100) ** 2

# Set the RGB values
for y in range(img.shape[0]):
    for x in range(img.shape[1]):
        r, g, b = circle[y][x], circle[y][x], circle[y][x]
        img[y][x][0] = 0
        img[y][x][1] = 0
        img[y][x][2] = 0
        img[y][x][3] = 0

def number_size(num):
    num_str = str(num)
    return (len(num_str)*2-1)*num_size*3, num_size*4

def draw_number(img, tile_x, tile_y, num):
    num_x, num_y = number_size(num)
    start_x = tile_x*tile_size+(tile_size-num_x)//2
    start_y = tile_y*tile_size+(tile_size-num_y)//2
    num_str = str(num)
    for index, number in enumerate(num_str):
        num_rep = representations[number]
        num_start_x = start_x+num_size*4*index
        
        for y in range(len(num_rep)):
            for x in range(len(num_rep[y])):
                for sy in range(num_size):
                    for sx in range(num_size):
                        v = num_rep[y][x]
                        if v == "#":
                            img[start_y+y*num_size+sy][num_start_x+x*num_size+sx][0] = 255
                            img[start_y+y*num_size+sy][num_start_x+x*num_size+sx][1] = 0
                            img[start_y+y*num_size+sy][num_start_x+x*num_size+sx][2] = 0
                            img[start_y+y*num_size+sy][num_start_x+x*num_size+sx][3] = 255

num = representations['0']
num_x = 3
num_y = 3
size = 2
#for y in range(len(num)):
#    for x in range(len(num[y])):
#        for sy in range(size):
#            for sx in range(size):
#                v = num[y][x]
#                if v == "#":
#                    img[num_y+y*size+sy][num_x+x*size+sx][0] = 255
#                    img[num_y+y*size+sy][num_x+x*size+sx][1] = 0
#                    img[num_y+y*size+sy][num_x+x*size+sx][2] = 0 
for i in range(100):
    x = i%10
    y = i//10
    draw_number(img, x, y, i+1)
# Display the image
#scipy.misc.imshow(img)

# Save the image
imageio.imwrite("number_tileset.png", img)

for y in range(img.shape[0]):
    for x in range(img.shape[1]):
        r, g, b = circle[y][x], circle[y][x], circle[y][x]
        img[y][x][0] = 0
        img[y][x][1] = 0
        img[y][x][2] = 0
        img[y][x][3] = 0

def draw_checker(img, tile_x, tile_y):
    start_x = tile_x*tile_size
    start_y = tile_y*tile_size
    for y in range(tile_size):
        for x in range(tile_size):
            even = (tile_x+tile_y)%2
            color = 255 if even else 0
            img[start_y+y][start_x+x][0] = color
            img[start_y+y][start_x+x][1] = color
            img[start_y+y][start_x+x][2] = color
            img[start_y+y][start_x+x][3] = 255

for i in range(100):
    x = i%10
    y = i//10
    draw_checker(img, x, y)

imageio.imwrite("checker_tileset.png", img)


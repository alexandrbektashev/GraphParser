import urllib.request
from PIL import Image
import os
import datetime
from datetime import timedelta
from operator import itemgetter


def get_images():
    if os.path.exists(".\data") == False:
        os.mkdir(".\data")
    print('Print two dates the beginning and the end. Data format dd.mm.yyyy')
    first_date = input()
    last_date = input()
    f_date = datetime.datetime.strptime(first_date, '%d.%m.%Y')
    l_date = datetime.datetime.strptime(last_date, '%d.%m.%Y')

    while l_date >= f_date:
                   url = "http://cliware.meteo.ru/webchart/timeser/26038/SYNOPRUS/TempDb24/2000/1200?colors=255,255,255;255,255,255;255,255,255;255,255,255;255,255,255;0,0,0;255,255,255&dates={0}-{1}-{2},{3}-{4}-{5}" \
                       .format(f_date.year, f_date.month, f_date.day,
                               f_date.year, f_date.month, f_date.day)
                   name = "data\{}_{}_{}.jpg".format(f_date.year, f_date.month,
                                                            f_date.day)
                   print ("Picture:", name)
                   img = urllib.request.urlopen(url).read()
                   out = open(name, "wb")
                   out.write(img)
                   out.close()

                   f_date = f_date + timedelta(days=1)


if __name__ == "__main__":
     get_images()
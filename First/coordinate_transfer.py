# coding: utf-8
# version: python3.6.1
# author: Yuhao Kang
import requests
import json


# Research region
class LocationDivide(object):
    def __init__(self, bound, size):
        # minLat,minLon,maxLat,maxLon
        self.minLat = float(str(bound).split(',')[0])
        self.minLon = float(str(bound).split(',')[1])
        self.maxLat = float(str(bound).split(',')[2])
        self.maxLon = float(str(bound).split(',')[3])
        self.size = size

    # Seperate block into blocks
    def compute_block(self):
        if (self.maxLat - self.minLat) % self.size == 0:
            lat_count = (self.maxLat - self.minLat) / self.size
        else:
            lat_count = (self.maxLat - self.minLat) / self.size + 1
        if (self.maxLon - self.minLon) % self.size == 0:
            lon_count = (self.maxLon - self.minLon) / self.size
        else:
            lon_count = (self.maxLon - self.minLon) / self.size + 1
        self.bounds = []
        lat_count = int(lat_count)
        lon_count = int(lon_count)

        try:
            for i in range(0, lat_count):
                for j in range(0, lon_count):
                    # maxLat,minLon,minLat,maxLon
                    minLat = self.minLat + i * self.size
                    minLon = self.minLon + j * self.size
                    maxLat = self.minLat + (i + 1) * self.size
                    if maxLat > self.maxLat:
                        maxLat = self.maxLat
                    maxLon = self.minLon + (j + 1) * self.size
                    if maxLon > self.maxLon:
                        maxLon = self.maxLon
                    # minLat,minLon,maxLat,maxLon
                    # set decimal
                    digit_number = 5
                    minLat = round(minLat, digit_number)
                    minLon = round(minLon, digit_number)
                    maxLat = round(maxLat, digit_number)
                    maxLon = round(maxLon, digit_number)
                    bound = "{0},{1},{2},{3}".format(minLat, minLon, maxLat, maxLon)
                    self.bounds.append(bound)
        except Exception as e:
            with open("e:log.txt", 'a') as log:
                log.writelines(e)
        return self.bounds

    # Write coordinates into files
    def write_coordinate(self):
        with open('coordinate.csv', 'w') as file:
            count = 1
            for coordinate in self.bounds:
                file.write("{0},{1}|".format(str(coordinate).split(',')[1], str(coordinate).split(',')[0]))
                # file.write(coordinate[0] + "\t")
                if count % 40 == 0:
                    file.write("\n")
                    count = 0
                count = count + 1


# Amap API request
class AmapAPI(object):
    def __init__(self):
        # Your amap api key
        self.api_key = "5892a17e4899dd11bd69d6eea7361187"
        self.convert_api = "http://restapi.amap.com/v3/assistant/coordinate/convert?"

    # Load all data in file
    def load_all_data(self):
        file = open('coordinate.csv', 'r')
        for line in file.readlines():
            self.convert_coordinate(line)

    # Convert WGS84 data to GCJ02 data
    def convert_coordinate(self, locations):
        locations = locations[:-2]
        params = {
            "key": self.api_key,
            "locations": str(locations),
            "coordsys": "gps"
        }
        try:
            r = requests.get(self.convert_api, params)
            # Get GCJ02 data
            json_data = json.loads(r.text)
            # Write into files
            with open('transfer.csv', 'a') as file:
                file.writelines(json_data['locations'])
                file.write('\n')
        except Exception as e:
            print(e)


class POINT(object):
    def __init__(self,point):
        self.x=float(str(point).split(',')[0])
        self.y=float(str(point).split(',')[1])

    def format_point(self):
        self.x=round(self.x,5)
        self.y=round(self.y,5)
        self.output="{0},{1}\n".format(self.x,self.y)


class coordinate_file(object):
    def __init__(self,filename,output):
        self.filename=filename
        self.file=open(filename,'r')
        self.output=open(output,'a')

    def format_file(self,code):
        for line in self.file.readlines():
            for row in line.split('\n'):
                for point in row.split(code):
                    if point != "":
                        pt=POINT(point)
                        pt.format_point()
                        self.output.writelines(pt.output)


if __name__ == '__main__':
    # Set region bound and interval
    # minLat,minLon,maxLat,maxLon,interval
    region = "39.915,116.405,39.975,116.415"
    location = LocationDivide(region, 0.001)
    # Seperate grid into blocks
    location.compute_block()
    # Read coordinates
    location.write_coordinate()
    # Convert coordinates
    amap = AmapAPI()
    amap.load_all_data()
    # Format files into 2 numbers in one line
    source_file=coordinate_file('coordinate.csv','coordinate_format.csv')
    source_file.format_file('|')
    transfer_file=coordinate_file('transfer.csv','transfer_format.csv')
    transfer_file.format_file(';')

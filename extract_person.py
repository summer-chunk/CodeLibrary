import os
from PIL import Image

save_dir = '/media/data4/wanglei/ReID_TEST/person224x224'
# save_dir = '/media/data4/wanglei/ReID_TEST/singlePerson'
       
# count = 0 
personCnt = {}
# countPerson = 0
air_idx, fixed_idx = 0, 0
air_list = open('City1M_V2_air/train1_allCount.txt').readlines()
fixed_list = open('City1M_V2_fixed/train1_allCount.txt').readlines()
for groupid in range(500):
    while air_list[air_idx].split(' ')[1] == str(groupid):
        path = air_list[air_idx].split(' ')[0]
        boxs = air_list[air_idx].split(' ')[2:-1]
        img = Image.open("City1M_V2_air" + path)
        for curBox in boxs:
            personID = curBox.split(',')[0]
            box = (float(curBox.split(',')[1]), 
                   float(curBox.split(',')[2]), 
                   float(curBox.split(',')[1]) + float(curBox.split(',')[3]), 
                   float(curBox.split(',')[2]) + float(curBox.split(',')[4]))
            if personID not in personCnt.keys():
                personCnt[personID] = 1
                save_path = save_dir + '/cam' + path[5] + '_' + personID + '_0.jpg'
                img.crop(box).resize((224, 224)).save(save_path)
            else:
                save_path = save_dir + '/cam' + path[5] + '_' + personID + '_' \
                            + str(personCnt[personID]) + '.jpg'
                img.crop(box).resize((224, 224)).save(save_path)
                personCnt[personID] += 1

        air_idx += 1
    
    while fixed_list[fixed_idx].split(' ')[1] == str(groupid):
        path = fixed_list[fixed_idx].split(' ')[0]
        boxs = fixed_list[fixed_idx].split(' ')[2:-1]
        img = Image.open("City1M_V2_fixed" + path)
        for curBox in boxs:
            personID = curBox.split(',')[0]
            box = (float(curBox.split(',')[1]), 
                   float(curBox.split(',')[2]), 
                   float(curBox.split(',')[1]) + float(curBox.split(',')[3]), 
                   float(curBox.split(',')[2]) + float(curBox.split(',')[4]))
            if personID not in personCnt.keys():
                personCnt[personID] = 1
                save_path = save_dir + '/cam' + str(int(path[5]) + 5) + '_' + personID + '_0.jpg'
                img.crop(box).resize((224, 224)).save(save_path)
            else:
                save_path = save_dir + '/cam' + str(int(path[5]) + 5) + '_' + personID + '_' \
                            + str(personCnt[personID]) + '.jpg'
                img.crop(box).resize((224, 224)).save(save_path)
                personCnt[personID] += 1
        fixed_idx += 1
    
    # count += 1
    print('~~~ Finsh train group =', groupid)
print('*' * 100)

for groupid in range(500, 800):
    while air_list[air_idx].split(' ')[1] == str(groupid):
        path = air_list[air_idx].split(' ')[0]
        boxs = air_list[air_idx].split(' ')[2:-1]
        img = Image.open("City1M_V2_air" + path)
        for curBox in boxs:
            personID = curBox.split(',')[0]
            box = (float(curBox.split(',')[1]), 
                   float(curBox.split(',')[2]), 
                   float(curBox.split(',')[1]) + float(curBox.split(',')[3]), 
                   float(curBox.split(',')[2]) + float(curBox.split(',')[4]))
            if personID not in personCnt.keys():
                if path[5] == '1':
                    save_path ='test224x224/query_overhead/cam' + path[5] + '_' + personID + '_0.jpg'
                    img.crop(box).resize((224, 224)).save(save_path)
                    save_path ='test224x224/gallery_air/cam' + path[5] + '_' + personID + '_0.jpg'
                    img.crop(box).resize((224, 224)).save(save_path)
                else:
                    save_path ='test224x224/gallery_side/cam' + path[5] + '_' + personID + '_0.jpg'
                    img.crop(box).resize((224, 224)).save(save_path)
                    save_path ='test224x224/gallery_air/cam' + path[5] + '_' + personID + '_0.jpg'
                    img.crop(box).resize((224, 224)).save(save_path)
                personCnt[personID] = 1
            else:
                if path[5] == '1':
                    save_path ='test224x224/query_overhead/cam' + path[5] + '_' + personID + '_' \
                               + str(personCnt[personID]) + '.jpg'
                    img.crop(box).resize((224, 224)).save(save_path)
                    save_path ='test224x224/gallery_air/cam' + path[5] + '_' + personID + '_' \
                               + str(personCnt[personID]) + '.jpg'
                    img.crop(box).resize((224, 224)).save(save_path)
                else:
                    save_path ='test224x224/gallery_side/cam' + path[5] + '_' + personID + '_' \
                               + str(personCnt[personID]) + '.jpg'
                    img.crop(box).resize((224, 224)).save(save_path)
                    save_path ='test224x224/gallery_air/cam' + path[5] + '_' + personID + '_' \
                               + str(personCnt[personID]) + '.jpg'
                    img.crop(box).resize((224, 224)).save(save_path)
                personCnt[personID] += 1

        air_idx += 1
        
    while fixed_list[fixed_idx].split(' ')[1] == str(groupid):
        path = fixed_list[fixed_idx].split(' ')[0]
        boxs = fixed_list[fixed_idx].split(' ')[2:-1]
        img = Image.open("City1M_V2_fixed" + path)
        for curBox in boxs:
            personID = curBox.split(',')[0]
            box = (float(curBox.split(',')[1]), 
                   float(curBox.split(',')[2]), 
                   float(curBox.split(',')[1]) + float(curBox.split(',')[3]), 
                   float(curBox.split(',')[2]) + float(curBox.split(',')[4]))
            if personID not in personCnt.keys():
                personCnt[personID] = 1
                save_path = 'test224x224/query_fixed/cam' + str(int(path[5]) + 5) + '_' + personID + '_0.jpg'
                img.crop(box).resize((224, 224)).save(save_path)
            else:
                save_path = 'test224x224/query_fixed/cam' + str(int(path[5]) + 5) + '_' + personID + '_' \
                            + str(personCnt[personID]) + '.jpg'
                img.crop(box).resize((224, 224)).save(save_path)
                personCnt[personID] += 1
        fixed_idx += 1
        
    print('~~~ Finsh test group =', groupid)
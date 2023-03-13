import os
import numpy as np

# threshold = 2500
threshold = 2500

group_list = {}
group_famliy = {}

# begin_personid 为起始组的 id，end_personid 为结束组的 id，preCount 是当前组之前已经分好的组数量，sampleNum 表示需要选的组数
def build_group(sample_list, personCount, preCount, sampleNum):
    curSelectCnt = 0 # 表示当前选的组数
    s = set()
    while curSelectCnt < sampleNum:
        if preCount + curSelectCnt * personCount < threshold:
            sample_group = np.random.choice(sample_list, personCount, replace=False) # 无放回
            sample_list = np.setdiff1d(sample_list, sample_group)            
            curSelectCnt += 1
            sample_group.sort()
            
            personSlide = ''
            for personid in sample_group:
                if len(personSlide) == 0:
                    personSlide = str(personid)
                else:
                    personSlide = personSlide + ',' + str(personid)
            group_list[personSlide] = True
            saveFamliyTree(personSlide)
        else: # > threshold
            # sample_list = np.arange(1, 2500)
            sample_list = np.arange(2501, 5000)
            sample_group = np.random.choice(sample_list, personCount, replace=True) # 有放回
            sample_group.sort()
            if checkVaildGroup(sample_group):
                personSlide = ''
                for personid in sample_group:
                    if len(personSlide) == 0:
                        personSlide = str(personid)
                    else:
                        personSlide = personSlide + ',' + str(personid)
                group_list[personSlide] = True
                saveFamliyTree(personSlide)
                curSelectCnt += 1
            else:
                continue
        
    return preCount + sampleNum * personCount
    
def saveFamliyTree(personSlide):
    persons = personSlide.split(',')
    n = len(persons)
    for i in range(2**n):
        substr = ""
        for j in range(n):
            if (i >> j) % 2 == 1:
                if len(substr) == 0:
                    substr = persons[j]
                else:
                    substr = substr + ',' + persons[j]
        if len(substr) == 0 or len(substr) == 1:
            continue
        subpersons = substr.split(',')
        intersection = np.intersect1d(persons, subpersons)
        union = np.union1d(persons, subpersons)
        if float(float(len(intersection)) / float(len(union)) >= 0.6):
            group_famliy[substr] = True
    
    
    
def checkVaildGroup(sample_group):
    # sample_group 是排序好的 numpy array
    personSlide = ''
    for personid in sample_group:
        if len(personSlide) == 0:
            personSlide = str(personid)
        else:
            personSlide = personSlide + ',' + str(personid)
    personSlide = personSlide.split(',')
    for key in group_famliy.keys():
        famliy_member = key.split(',')
        intersection = np.intersect1d(famliy_member, personSlide)
        union = np.union1d(famliy_member, personSlide)
        if float(float(len(intersection)) / float(len(union)) >= 0.6):
            return False
    return True


beginIndex = 2501
group2 = 20
group3 = 30
group4 = 41
group5 = 50
group6 = 500 - group2 - group3 - group4 - group5
# print('Used persons =', group2*2+group3*3+group4*4+group5*5+group6*6)
sample_list2 = [x for x in range(0 + beginIndex, group2 * 2 + beginIndex)]
sample_list3 = [x for x in range(group2 * 2 + beginIndex, group2 * 2 + group3 * 3 + beginIndex)]
sample_list4 = [x for x in range(group2 * 2 + group3 * 3 + beginIndex, group2 * 2 + group3 * 3 + group4 * 4 + beginIndex)]
sample_list5 = [x for x in range(group2 * 2 + group3 * 3 + group4 * 4 + beginIndex, \
                group2 * 2 + group3 * 3 + group4 * 4 + group5 * 5 + beginIndex)]
sample_list6 = [x for x in range(group2 * 2 + group3 * 3 + group4 * 4 + group5 * 5 + beginIndex, threshold + beginIndex)]

print(sample_list2[0], sample_list2[-1])
print(sample_list3[0], sample_list3[-1])
print(sample_list4[0], sample_list4[-1])
print(sample_list5[0], sample_list5[-1])
print(sample_list6[0], sample_list6[-1])

countPerson2 = build_group(sample_list2, 2, 0, group2)
print('~~~ Finsh split 2 persons\'s group, and finish ' + str(countPerson2) + ' groups ~~~')
countPerson3 = build_group(sample_list3, 3, countPerson2, group3)
print('~~~ Finsh split 3 persons\'s group, and finish ' + str(countPerson3) + ' groups ~~~')
countPerson4 = build_group(sample_list4, 4, countPerson3, group4)
print('~~~ Finsh split 4 persons\'s group, and finish ' + str(countPerson4) + ' groups ~~~')
countPerson5 = build_group(sample_list5, 5, countPerson4, group5)
print('~~~ Finsh split 5 persons\'s group, and finish ' + str(countPerson5) + ' groups ~~~')
countPerson6 = build_group(sample_list6, 6, countPerson5, group6)
print('~~~ Finsh split 6 persons\'s group, and finish ' + str(countPerson6) + ' groups ~~~')
print('-----------------------------------')
print('Split group number =', len(group_list))
# a = np.arange(10)
# print(a)
# b = np.array([4,5,8])
# print(b)
# a = np.setdiff1d(a, b)
# print(a)
print('Begin writting txt file!')
with open('toyDatatset_test.txt', 'w') as f:
    for key in group_list.keys():
        f.write(key)
        f.write('\n')
        # f.write(',\n')
f.close()

# f2 = open('toyDatatset_train_groupSplits.txt', 'r').readlines()
# s = set()
# for personSlides in f2:
#     persons = personSlides.split(',')
#     for person in persons:
#         s.add(person)
# print('different person id in txt =', len(s))
s = set()
for key in group_list.keys():
    persons = key.split(',')
    for person in persons:
        s.add(person)
print('different person id in dict =', len(s))
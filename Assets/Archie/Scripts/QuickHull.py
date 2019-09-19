# QuickHull2D testing in Python
import math
class Vector:
    def __init__(self, ix = 0, iy = 0):
        self.x = ix
        self.y = iy

    def __add__(self, other):
        return Vector(self.x + other.x, self.y + other.y)

    def __sub__(self, other):
        return Vector(self.x - other.x, self.y - other.y)

    def __str__(self):
        return "2D Vector( x: " + str(self.x) + ", y: " + str(self.y) + " )"

    def __eq__(self, other):
        return (self.x == other.x) and (self.y == other.y)

    def normalize(self):
        if self.magnitude() == 0:
            print "ERROR: cannot normalise zero verctor"
        else:
            nx = self.x / self.magnitude()
            ny = self.y / self.magnitude()
            return Vector(nx, ny)

    def magnitude(self):
        return math.sqrt(math.pow(self.x,2) + math.pow(self.y,2))

def distance(vector1, vector2):
    return math.sqrt(math.pow(vector1.x - vector2.x,2) + math.pow(vector1.y - vector2.y,2))

def dot(vector1, vector2):
    return vector1.x * vector2.x + vector1.y * vector2.y

def distance_from_line(base_of_line, direction_of_line, position, line_wise=True):
    point2point = position - base_of_line
    if ( base_of_line + direction_of_line == position):
        return 0
    if direction_of_line.magnitude() > 0 :
        direction = direction_of_line.normalize()
        projection_length = dot(point2point,direction)
        if (projection_length > direction_of_line.magnitude() or projection_length < 0) and line_wise:   #these two lines prevent closest ones been chosen which are not within line length (in other words, if line is finite)
            return float("inf")
        returning = math.pow(point2point.magnitude(),2) - math.pow(projection_length,2)
        if returning >= 0 :
            return math.sqrt(returning)
        else:
            print "ERROR: " + str(returning) + " BASE: " + str(base_of_line) + " DIRECTION: " + str(direction_of_line) + " Position: " + str(position) + "  Project Length: " + "{0:.20f}".format(projection_length) + " point to point length: " + "{0:.20f}".format(point2point.magnitude())
            print (projection_length == point2point.magnitude())
            print (projection_length - point2point.magnitude())
            return 0

# """
# TESTING LINE DISTANCE CALCULATION
# """
# a = Vector(10,-5)
# b = Vector(10,5)
# o = Vector(-1,0)
# # o = Vector(-1,10)
# print "IMPORTANT TEST: " + str(distance_from_line(a,b-a,o))

# """
# TESTING DOT CALCULATION
# """
# a = Vector(1,0)
# b = Vector(-1,0)
# print "IMPORTANT TEST: " + str(dot(a,b))
# """
# TESTING Normalisation CALCULATION
# """
# a = Vector(10,10)
# print "IMPORTANT TEST: " + str(a.normalize())
# print "IMPORTANT TEST: " + str(a.normalize().magnitude())
# """
# TESTING Magnitude
# """
# a = Vector(10,10)
# print "IMPORTANT TEST: " + str(a.magnitude())



points = []

# randomly generating 20 points
import random
seed= 14
#error list: 1, 3,5,6,(8),9,13,(14)
#with recent changes, 1, 3, 5, 6, 8, 9, 13, 14 are tested and working correctly
print "Rand Seed: " + str(seed)
random.seed(seed)

for i in range(0,1000):
    points.append(Vector(random.randint(-200,200),random.randint(-200,200)))
    print points[len(points)-1]

# drawing points using turtle
import turtle

def draw_points(points, active = False):
    vertex_marker = turtle
    vertex_marker.speed(0)
    vertex_marker.penup()
    for p in points:
        vertex_marker.setpos(p.x,p.y)
        if not active:
            vertex_marker.dot(5)
        else:
            vertex_marker.dot(5,'red')
    vertex_marker.pendown()
    
draw_points(points)

import time
time.sleep(5)


# finding maximal 2D simplex (trinagle)
base = [Vector(0,0),Vector(0,0)]
third = Vector(0,0)
for p1 in points:
    for p2 in points:
        if distance(p1,p2) > distance(base[0],base[1]):
            base[0] = p1
            base[1] = p2

direction = base[1] - base[0]
max_distance = base[0]
for p in points:
    current = distance_from_line(base[0],direction,max_distance) 
    testing = distance_from_line(base[0],direction,p)
    if testing > current:
        print "ding"
        max_distance = p
        print max_distance
        print current
        print testing
third = max_distance

triangle_corners = [base[0],base[1],third]
print triangle_corners

# drawing triangle (maximal simplex, first hull)
def draw_triangle(three_corners):
    triangle_drawer = turtle
    triangle_drawer.penup()
    triangle_drawer.setpos(three_corners[0].x, three_corners[0].y)
    triangle_drawer.pendown()
    triangle_drawer.setpos(three_corners[1].x, three_corners[1].y)
    triangle_drawer.setpos(three_corners[2].x, three_corners[2].y)
    triangle_drawer.setpos(three_corners[0].x, three_corners[0].y)
    triangle_drawer.penup()

def draw_active_edge(startvector, endvector, active):
    edge_drawer = turtle
    edge_drawer.penup()
    if active:
        edge_drawer.pen(pencolor='red')
    else:
        edge_drawer.pen(pencolor='black')
    edge_drawer.setpos(startvector.x, startvector.y)
    edge_drawer.pendown()
    edge_drawer.setpos(endvector.x, endvector.y)
    edge_drawer.pen(pencolor='black')
    edge_drawer.penup()

def draw_complete_edge(startvector, endvector, active = True):
    edge_drawer = turtle
    edge_drawer.penup()
    if active:
        edge_drawer.pen(pencolor='blue')
    else:
        edge_drawer.pen(pencolor='black')
    edge_drawer.setpos(startvector.x, startvector.y)
    edge_drawer.pendown()
    edge_drawer.setpos(endvector.x, endvector.y)
    edge_drawer.pen(pencolor='black')
    edge_drawer.penup()

draw_triangle(triangle_corners)

# removing points inside maximal simplex
import numpy
def clear_simplex_area(points, triangle_corners):
    # using linear algebra and barycentric coordinates
    exterior_vertices = []
    v = triangle_corners[1] - triangle_corners[0]
    u = triangle_corners[2] - triangle_corners[0]
    matrix = numpy.array([[u.x,v.x],[u.y,v.y]])
    if numpy.linalg.det(matrix) != 0:
        # print matrix
        inverse = numpy.linalg.inv(matrix)
        for p in points:
            if p not in triangle_corners:
                lhs = p - triangle_corners[0]
                lhs = numpy.array([[lhs.x],[lhs.y]])
                barycentric = inverse.dot(lhs)
                # print barycentric
                if barycentric[0] < 0 or barycentric[0] > 1 or barycentric[1] < 0 or barycentric[1] > 1 or (1 - barycentric[0] - barycentric[1] ) < 0:
                    exterior_vertices.append(p)
        return exterior_vertices
    else:
        return []

points = clear_simplex_area(points, triangle_corners)
# draw_points(points)
draw_triangle(triangle_corners)

time.sleep(5)

class face:
    def __init__(self, A, B):
        self.conflict_lists = {}
        self.vertexA = A
        self.vertexB = B
        self.complete = False


edge_list = []
for i in range(0,len(triangle_corners)):
    edge_list.append( face(triangle_corners[i], triangle_corners[(i+1)%3]))

def distribute_conflicts(points, edge_list):
    for p in points:
        distances = []
        for edge in edge_list:
            distances.append(distance_from_line(edge.vertexA, edge.vertexB - edge.vertexA, p))
        edge_list[distances.index(min(distances))].conflict_lists[min(distances)] = p

distribute_conflicts(points, edge_list)

def it(edge_list):
    new_edge_list = []
    for edge in edge_list:
        time.sleep(1)
        sorted_points = sorted(edge.conflict_lists)
        draw_active_edge(edge.vertexA, edge.vertexB, True)
        draw_points(edge.conflict_lists.values(), True)
        time.sleep(2)
        draw_active_edge(edge.vertexA, edge.vertexB, False)
        if len(sorted_points) >= 1:
            furthest_from_face = edge.conflict_lists[sorted_points.pop()]
            corners = [edge.vertexA, edge.vertexB, furthest_from_face ]
            # for a in edge.conflict_lists.values():
            #     print a
            # print "fin"
            remaining_conflicts = clear_simplex_area(edge.conflict_lists.values(), corners)
            draw_triangle(corners)
            draw_points(edge.conflict_lists.values(), False)
            draw_points(remaining_conflicts, True)
            time.sleep(2)
            draw_points(remaining_conflicts)
            daughter_edges = [face(edge.vertexA, furthest_from_face), face(edge.vertexB, furthest_from_face)] 
            distribute_conflicts(remaining_conflicts, daughter_edges)
            for daughter in daughter_edges:
                if len(daughter.conflict_lists) <= 0: 
                    daughter.complete = True
                    draw_complete_edge(daughter.vertexA, daughter.vertexB)
            new_edge_list.append(daughter_edges[0])
            new_edge_list.append(daughter_edges[1])
        else:
            print "skip"
            new_edge_list.append(edge)
            edge.complete = True
            draw_complete_edge(edge.vertexA, edge.vertexB)
    return new_edge_list


all_complete = False
while len(edge_list) > 0:
    print "round"
    edge_list = it(edge_list)
    all_complete = True
    new_edge_list = []
    for edge in edge_list:
        if not edge.complete:
            new_edge_list.append(edge)
    edge_list = new_edge_list
# it(it(it(it(it(edge_list)))))



time.sleep(10)

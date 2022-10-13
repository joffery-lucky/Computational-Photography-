#---------------------------------------------------------------------------------------------------
# CMake Build Configuration for the Ray Tracing Weekend Series
#
# See README.md for guidance.
#---------------------------------------------------------------------------------------------------

cmake_minimum_required ( VERSION 3.1.0 )

project ( RTWeekend
  VERSION 3.0.0
  LANGUAGES CXX
)

# Set to c++11
set ( CMAKE_CXX_STANDARD 11 )

# Source
set ( COMMON_ALL
  src/common/rtweekend.h
  src/common/camera.h
  src/common/ray.h
  src/common/vec3.h
)

set ( SOURCE_NEXT_WEEK
  ${COMMON_ALL}
  src/common/aabb.h
  src/common/texture.h
  src/TheNextWeek/aarect.h
  src/TheNextWeek/box.h
  src/TheNextWeek/hittable.h
  src/TheNextWeek/hittable_list.h
  src/TheNextWeek/material.h
  src/TheNextWeek/sphere.h
  src/TheNextWeek/main.cc
)


# Executables
add_executable(theNextWeek       ${SOURCE_NEXT_WEEK})

include_directories(src/common)

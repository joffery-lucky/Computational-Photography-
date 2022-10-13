#include "rtweekend.h"
#include "box.h"
#include "camera.h"
#include "color.h"
#include "hittable_list.h"
#include "material.h"
#include "sphere.h"
#include "texture.h"

#include <iostream>


color ray_color(const ray& r, const color& background, const hittable& world, int depth) {
    hit_record rec;

    if (depth <= 0)
        return color(0, 0, 0);

    if (!world.hit(r, 0.001, infinity, rec))
        return background;

    ray scattered;
    color attenuation;
    color emitted = rec.mat_ptr->emitted(rec.u, rec.v, rec.p);

    if (!rec.mat_ptr->scatter(r, rec, attenuation, scattered))
        return emitted;

    return emitted + attenuation * ray_color(scattered, background, world, depth - 1);
}

hittable_list cornell_box() {
    hittable_list objects;

    auto red   = make_shared<lambertian>(color(.65, .05, .05));
    auto white = make_shared<lambertian>(color(.73, .73, .73));
    auto green = make_shared<lambertian>(color(.12, .45, .15));
    auto light = make_shared<diffuse_light>(color(15, 15, 15));

    auto loc0 = random_double(100, 300);
    auto width = random_double(80, 140);
    auto loc1 = random_double(150, 350);
    auto length = random_double(90, 150);

    auto albedo0 = color::random(0.5, 1);
    auto fuzz = random_double(0, 0.5);
    auto mental_material = make_shared<metal>(albedo0, fuzz);
    auto albedo1 = color::random() * color::random();
    auto lambertian_material = make_shared<lambertian>(albedo1);

    auto plight= make_shared<point_light>(color(15000, 15000, 15000),point3(278, 554, 270));
   
    objects.add(make_shared<yz_rect>(0, 555, 0, 555, 555, green));
    objects.add(make_shared<yz_rect>(0, 555, 0, 555, 0, red));
    objects.add(make_shared<xz_rect>(0, 555, 0, 555, 555, white));
    objects.add(make_shared<xz_rect>(0, 555, 0, 555, 0, white));
    objects.add(make_shared<xy_rect>(0, 555, 0, 555, 555, white));

    //objects.add(make_shared<xz_rect>(loc0, loc0 + width, loc1, loc1 + length, 554, light));
    objects.add(make_shared<yz_rect>(loc0, loc0 + width, loc1, loc1 + length, 554, light));

    shared_ptr<hittable> box1 = make_shared<box>(point3(0,0,0), point3(165,330,165), white);
    box1 = make_shared<rotate_y>(box1, 15);
    box1 = make_shared<translate>(box1, vec3(265,0,295));
    objects.add(box1);

    shared_ptr<hittable> box2 = make_shared<box>(point3(0,0,100), point3(165,165,265), white);
    objects.add(box2);
    
    objects.add(make_shared<sphere>(point3(278, 554, 270), 20, plight));
    objects.add(make_shared<sphere>(point3(250, 70, 140), 70, mental_material));
    objects.add(make_shared<sphere>(point3(40, 216, 190), 50, lambertian_material));
    objects.add(make_shared<sphere>(point3(390, 60, 70), 60, make_shared<dielectric>(1.5)));

    return objects;
}



int main() {

    // Image
    auto aperture = 0.0;
    color background(0,0,0);
    auto world = cornell_box();
    auto aspect_ratio = 1.0;
    int image_width = 600;
    int samples_per_pixel = 200;
    double x = random_double(100, 350);
    auto lookfrom = point3(x, x, -800);
    auto lookat = point3(278, 278, 0);
    double vfov = 40.0;
    int max_depth = 50;
            

    // Camera

    const vec3 vup(0,1,0);
    const auto dist_to_focus = 10.0;
    const int image_height = static_cast<int>(image_width / aspect_ratio);

    camera cam(lookfrom, lookat, vup, vfov, aspect_ratio, aperture, dist_to_focus);

    // Render

    std::cout << "P3\n" << image_width << ' ' << image_height << "\n255\n";

    for (int j = image_height-1; j >= 0; --j) {
        std::cerr << "\rScanlines remaining: " << j << ' ' << std::flush;
        for (int i = 0; i < image_width; ++i) {
            color pixel_color(0,0,0);
            for (int s = 0; s < samples_per_pixel; ++s) {
                auto u = (i + random_double()) / (image_width-1);
                auto v = (j + random_double()) / (image_height-1);
                ray r = cam.get_ray(u, v);
                pixel_color += ray_color(r, background, world, max_depth);
            }
            write_color(std::cout, pixel_color, samples_per_pixel);
        }
    }

    std::cerr << "\nDone.\n";
}

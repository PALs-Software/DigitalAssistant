$fn = 200;

InnerDiameter = 70;
Thickness = 4;
Height = 100;

ShowDummyESP = false;
ShowBase = false;
ShowTop = true;

SpeakerHeight = 30;
SpeakerDiameter = 40;
OuterDiameter = InnerDiameter + Thickness;
EspHeight = 45;

if (ShowBase){
    Base();
}

if (ShowTop){
    translate([0,0,Height])
    Top();
}

module Top(){
    
    if(ShowDummyESP){    
        translate([3,0,0])
        color("red"){
            translate([0,0,-36.5])
            cube([57,28, 12], center = true);
          
            translate([-32,0,-45])
            cube([7,13, 2], center = true);
        }
    }
    
    
    difference(){
        translate([0,0,-Thickness])
        cylinder(d = InnerDiameter-0.7, h = Thickness); 
        translate([0,0,-Thickness-1])
        cylinder(d = InnerDiameter-6, h = Thickness+2); 
    }
    
    difference(){
        cylinder(d = OuterDiameter, h = Thickness); 
    
        translate([InnerDiameter/2-2,0,-2])
        cube([7,5,4]);
        
        translate([0,0,-1])
        cylinder(d = 3, h = Thickness + 2); 
        
        translate([0,0,-5])
        cylinder(d = 15.5, h = Thickness + 2); 
                 
        translate([0,3.5,1])        
        cube([8,2,4+0], center = true);
        translate([0,-6+2.5,1])        
        cube([8,2,4+0], center = true);
    }
    
    translate([-1,0+24.5/2,-5])
    cube([3,15,5]);
    translate([-1,-5-24.5/2-11,-5])
    cube([3,15,5]);
    
    difference(){
        translate([-10,0+22.5/2,-EspHeight])
        cube([20,5,EspHeight]);
            
        translate([-10,0+22.5/2,-EspHeight+2])
        cube([20,2,13]);
    }
    
    difference(){
        translate([-10,-5-22.5/2,-EspHeight])
        cube([20,5,EspHeight]);
       
        translate([-10,-2-22.5/2,-EspHeight+2])
        cube([20,2,13]);
    }
   
}

module Base(){
    difference(){
        // Base Case
        cylinder(d = OuterDiameter, h = Height);
         
        
        translate([0,0,SpeakerHeight - 1])
        cylinder(d = InnerDiameter, h = Height + 2);  
      
        translate([0,0,-1])
        cylinder(d = InnerDiameter - 5, h = SpeakerHeight + 1);  
        
        // Bottom Holes
        for(i =[0:30:360])
        rotate([0,0,i])
        translate([0,0,9])
        cube([OuterDiameter + 2, 10, 20], center = true);
        
        for(i =[0:20:360])
        rotate([0,0,i])
        translate([0,0,80])
        cube([OuterDiameter + 2, 2, 20], center = true);
        
        // USB-C Hole
        translate([30,0,56])
        cube([20,26, 8], center = true);
    }

    // Speaker Holder
    difference(){
        translate([0,0,SpeakerHeight])
        cylinder(d = InnerDiameter, h = Thickness * 2); 

        translate([0,0,SpeakerHeight - 1])
        cylinder(d = SpeakerDiameter - 3, h = Thickness + 2); 
        
         translate([0,0,SpeakerHeight + Thickness])
        cylinder(d = SpeakerDiameter + 2, h = Thickness + 2); 
    }
    
   
}